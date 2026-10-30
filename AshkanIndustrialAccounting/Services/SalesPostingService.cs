using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Posts an existing draft invoice and issues stock in a single serializable transaction.</summary>
    public static class SalesPostingService
    {
        public static void Post(int invoiceId, string user)
        {
            AuthorizationService.RequireActiveSession("sales");
            if (!string.Equals(user, AuthorizationService.CurrentUserName, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Posting actor does not match signed-in user.");
            if (invoiceId <= 0) throw new ArgumentOutOfRangeException("invoiceId");
            if (String.IsNullOrWhiteSpace(user) || user.Length > 100) throw new ArgumentException("Valid user required");
            using (var cn = Db.Open())
            using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    string status;
                    using (var cmd = new SqlCommand("SELECT Status FROM dbo.SalesInvoices WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id", cn, tx))
                    {
                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = invoiceId;
                        status = cmd.ExecuteScalar() as string;
                    }
                    if (status == null) throw new InvalidOperationException("Invoice not found");
                    if (!String.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Only draft invoices can be posted");
                    var lines = new DataTable();
                    using (var cmd = new SqlCommand(@"SELECT ProductId, SUM(Qty) AS Qty FROM dbo.SalesInvoiceLines WHERE InvoiceId=@id GROUP BY ProductId ORDER BY ProductId", cn, tx))
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = invoiceId;
                        da.Fill(lines);
                    }
                    if (lines.Rows.Count == 0) throw new InvalidOperationException("Invoice has no lines");
                    // Fail before any inventory mutation when a draft is inconsistent.
                    ValidateInvoicePreflight(cn, tx, invoiceId);
                    foreach (DataRow line in lines.Rows)
                    {
                        int productId = Convert.ToInt32(line["ProductId"]);
                        decimal qty = Convert.ToDecimal(line["Qty"]);
                        if (qty <= 0) throw new InvalidOperationException("Invalid invoice quantity");
                        decimal before;
                        using (var cmd = new SqlCommand("SELECT StockQty FROM dbo.Products WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id AND IsActive=1", cn, tx))
                        {
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = productId;
                            var value = cmd.ExecuteScalar();
                            if (value == null) throw new InvalidOperationException("Product unavailable: " + productId);
                            before = Convert.ToDecimal(value);
                        }
                        decimal checkedAfter = FinancialRules.RemainingStock(before, qty);
                        // Valuation and physical stock are updated in the same transaction.
                        // Opening valuations must be reconciled before enabling posting.
                        Guid valuationOperation = Guid.NewGuid();
                        decimal valuationDelta = WeightedAverageValuationService.Apply(
                            cn, tx, valuationOperation, productId, "ISSUE", qty, 0m);
                        using (var cost = new SqlCommand(@"INSERT dbo.SalesCostSnapshots
(InvoiceId,ProductId,Quantity,UnitCost,TotalCost)
VALUES(@invoice,@product,@qty,@unit,@total)", cn, tx))
                        {
                            cost.Parameters.Add("@invoice", SqlDbType.Int).Value = invoiceId;
                            cost.Parameters.Add("@product", SqlDbType.Int).Value = productId;
                            DecimalParam(cost, "@qty", qty);
                            DecimalParam(cost, "@unit", Math.Round(-valuationDelta / qty, 2, MidpointRounding.AwayFromZero));
                            DecimalParam(cost, "@total", -valuationDelta);
                            cost.ExecuteNonQuery();
                        }
                        decimal after = checkedAfter;
                        using (var cmd = new SqlCommand("UPDATE dbo.Products SET StockQty=@qty WHERE Id=@id", cn, tx))
                        {
                            DecimalParam(cmd, "@qty", after);
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = productId;
                            if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Stock update failed");
                        }
                        using (var cmd = new SqlCommand(@"INSERT dbo.InventoryMovements(OperationId,ProductId,MovementType,QuantityDelta,StockBefore,StockAfter,ReferenceNo,Notes,PerformedBy) VALUES(NEWID(),@product,'OUT',@delta,@before,@after,@ref,@notes,@user)", cn, tx))
                        {
                            cmd.Parameters.Add("@product", SqlDbType.Int).Value = productId;
                            DecimalParam(cmd, "@delta", -qty);
                            DecimalParam(cmd, "@before", before);
                            DecimalParam(cmd, "@after", after);
                            cmd.Parameters.Add("@ref", SqlDbType.NVarChar, 80).Value = "SALE-" + invoiceId;
                            cmd.Parameters.Add("@notes", SqlDbType.NVarChar, 500).Value = "Invoice posting";
                            cmd.Parameters.Add("@user", SqlDbType.NVarChar, 100).Value = user;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    using (var cmd = new SqlCommand("UPDATE dbo.SalesInvoices SET Status='Posted' WHERE Id=@id AND Status='Draft'", cn, tx))
                    {
                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = invoiceId;
                        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Invoice state changed");
                    }
                    SalesJournalService.Record(cn, tx, invoiceId, false);
                    TransactionalAuditService.Record(cn, tx, user, "SalesInvoicePosted", "SalesInvoice", invoiceId.ToString(), "Status: Draft -> Posted; inventory issued; sales journal posted");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }
        private static void ValidateInvoicePreflight(SqlConnection cn, SqlTransaction tx, int invoiceId)
        {
            using (var cmd = new SqlCommand(@"SELECT Subtotal,Discount,Tax,Total
FROM dbo.SalesInvoices WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id", cn, tx))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = invoiceId;
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) throw new InvalidOperationException("Invoice not found");
                    FinancialRules.NetRevenue(reader.GetDecimal(0),reader.GetDecimal(1),
                        reader.GetDecimal(2),reader.GetDecimal(3));
                }
            }
            using (var cmd = new SqlCommand(@"SELECT
COUNT(*) AS LineCount,
COALESCE(SUM(LineTotal),0) AS LineSum,
SUM(CASE WHEN Qty<=0 OR UnitPrice<0 OR Discount<0 OR LineTotal<0 THEN 1 ELSE 0 END) AS InvalidLines
FROM dbo.SalesInvoiceLines WITH (HOLDLOCK) WHERE InvoiceId=@id", cn, tx))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = invoiceId;
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read() || reader.GetInt32(0)==0 ||
                        Convert.ToInt32(reader[2])>0)
                        throw new InvalidOperationException("Invalid invoice lines");
                }
            }
            // Protect against journals created by older versions or manual SQL edits.
            using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM dbo.JournalEntries WITH (UPDLOCK,HOLDLOCK)
WHERE EntryNo=@no OR ReferenceNo=@reference", cn, tx))
            {
                cmd.Parameters.Add("@no", SqlDbType.NVarChar,40).Value="SI-"+invoiceId;
                cmd.Parameters.Add("@reference", SqlDbType.NVarChar,80).Value="SALE-"+invoiceId;
                if (Convert.ToInt32(cmd.ExecuteScalar())!=0)
                    throw new InvalidOperationException("Sales journal already exists for invoice");
            }
        }

        private static void DecimalParam(SqlCommand cmd, string name, decimal value)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 18; p.Scale = 3; p.Value = value;
        }
    }
}
