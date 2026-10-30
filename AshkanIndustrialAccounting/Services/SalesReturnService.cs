using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;
namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Full invoice return. A posted invoice may be returned only once; stock and audit trail commit together.</summary>
    public static class SalesReturnService
    {
        public static void ReturnPosted(int invoiceId, string user)
        {
            AuthorizationService.RequireActiveSession("sales");
            if (!String.Equals(user, AuthorizationService.CurrentUserName, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Return user must match the active session.");
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
                    if (!String.Equals(status, "Posted", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Only posted invoices can be returned");
                    var lines = new DataTable();
                    using (var cmd = new SqlCommand("SELECT ProductId,SUM(Qty) Qty FROM dbo.SalesInvoiceLines WHERE InvoiceId=@id GROUP BY ProductId ORDER BY ProductId", cn, tx))
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = invoiceId;
                        da.Fill(lines);
                    }
                    if (lines.Rows.Count == 0) throw new InvalidOperationException("Invoice has no lines");
                    foreach (DataRow row in lines.Rows)
                    {
                        int productId = Convert.ToInt32(row["ProductId"]);
                        decimal quantity = Convert.ToDecimal(row["Qty"]);
                        if (quantity <= 0) throw new InvalidOperationException("Invalid return quantity");
                        decimal before;
                        using (var cmd = new SqlCommand("SELECT StockQty FROM dbo.Products WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id", cn, tx))
                        {
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = productId;
                            object result = cmd.ExecuteScalar();
                            if (result == null || result == DBNull.Value) throw new InvalidOperationException("Product missing");
                            before = Convert.ToDecimal(result);
                        }
                        decimal originalUnitCost;
                        using (var cost = new SqlCommand(@"SELECT TotalCost, Quantity FROM dbo.SalesCostSnapshots
WHERE InvoiceId=@invoice AND ProductId=@product", cn, tx))
                        {
                            cost.Parameters.Add("@invoice", SqlDbType.Int).Value = invoiceId;
                            cost.Parameters.Add("@product", SqlDbType.Int).Value = productId;
                            using (var reader = cost.ExecuteReader())
                            {
                                if (!reader.Read()) throw new InvalidOperationException("Missing original sale cost snapshot");
                                decimal originalTotal = reader.GetDecimal(0);
                                decimal originalQty = reader.GetDecimal(1);
                                if (originalQty != quantity) throw new InvalidOperationException("Return quantity mismatch");
                                originalUnitCost = originalTotal / originalQty;
                            }
                        }
                        WeightedAverageValuationService.Apply(cn, tx, Guid.NewGuid(),
                            productId, "RECEIPT", quantity, originalUnitCost);
                        decimal after = checked(before + quantity);
                        using (var cmd = new SqlCommand("UPDATE dbo.Products SET StockQty=@qty WHERE Id=@id", cn, tx))
                        {
                            Amount(cmd,"@qty",after);
                            cmd.Parameters.Add("@id",SqlDbType.Int).Value = productId;
                            if (cmd.ExecuteNonQuery()!=1) throw new InvalidOperationException("Stock update failed");
                        }
                        using (var cmd = new SqlCommand("INSERT dbo.InventoryMovements(OperationId,ProductId,MovementType,QuantityDelta,StockBefore,StockAfter,ReferenceNo,Notes,PerformedBy) VALUES(NEWID(),@product,'IN',@delta,@before,@after,@ref,@notes,@user)",cn,tx))
                        {
                            cmd.Parameters.Add("@product",SqlDbType.Int).Value=productId;
                            Amount(cmd,"@delta",quantity); Amount(cmd,"@before",before); Amount(cmd,"@after",after);
                            cmd.Parameters.Add("@ref",SqlDbType.NVarChar,80).Value="RETURN-"+invoiceId;
                            cmd.Parameters.Add("@notes",SqlDbType.NVarChar,500).Value="Full sales return";
                            cmd.Parameters.Add("@user",SqlDbType.NVarChar,100).Value=user;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    using(var cmd=new SqlCommand("UPDATE dbo.SalesInvoices SET Status='Returned' WHERE Id=@id AND Status='Posted'",cn,tx))
                    {
                        cmd.Parameters.Add("@id",SqlDbType.Int).Value=invoiceId;
                        if(cmd.ExecuteNonQuery()!=1) throw new InvalidOperationException("Invoice state changed");
                    }
                    SalesJournalService.Record(cn, tx, invoiceId, true);
                    TransactionalAuditService.Record(cn, tx, user, "SalesInvoiceReturned", "SalesInvoice",
                        invoiceId.ToString(), "Status: Posted -> Returned; inventory restored; reversal journal posted");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }
        private static void Amount(SqlCommand command,string name,decimal value)
        {
            var p=command.Parameters.Add(name,SqlDbType.Decimal);p.Precision=18;p.Scale=3;p.Value=value;
        }
    }
}
