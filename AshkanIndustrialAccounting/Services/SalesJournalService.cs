using System;
using System.Data;
using System.Data.SqlClient;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Creates a balanced, posted sales journal inside the caller's SQL transaction.</summary>
    internal static class SalesJournalService
    {
        internal static void Record(SqlConnection cn, SqlTransaction tx, int invoiceId, bool reversal)
        {
            string reference = (reversal ? "RETURN-" : "SALE-") + invoiceId;
            decimal total, subtotal, discount, tax;
            using (var cmd = new SqlCommand(
                "SELECT Total,Subtotal,Discount,Tax FROM dbo.SalesInvoices WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id",
                cn, tx))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = invoiceId;
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) throw new InvalidOperationException("Invoice not found");
                    total = reader.GetDecimal(0);
                    subtotal = reader.GetDecimal(1);
                    discount = reader.GetDecimal(2);
                    tax = reader.GetDecimal(3);
                }
            }
            decimal netRevenue = FinancialRules.NetRevenue(subtotal, discount, tax, total);
            int receivable = Account(cn, tx, "1200");
            int revenue = Account(cn, tx, "4000");
            int entryId;
            using (var cmd = new SqlCommand(@"INSERT dbo.JournalEntries(EntryNo,EntryDate,Description,ReferenceNo,IsPosted)
VALUES(@no,SYSUTCDATETIME(),@description,@reference,1);SELECT CAST(SCOPE_IDENTITY() AS INT)", cn, tx))
            {
                cmd.Parameters.Add("@no", SqlDbType.NVarChar, 40).Value = (reversal ? "SR-" : "SI-") + invoiceId;
                cmd.Parameters.Add("@description", SqlDbType.NVarChar, 500).Value = reversal ? "Sales return" : "Sales invoice";
                cmd.Parameters.Add("@reference", SqlDbType.NVarChar, 80).Value = reference;
                entryId = (int)cmd.ExecuteScalar();
            }
            Line(cn, tx, entryId, receivable, reversal ? 0 : total, reversal ? total : 0);
            if (netRevenue > 0m)
                Line(cn, tx, entryId, revenue, reversal ? netRevenue : 0m, reversal ? 0m : netRevenue);
            if (tax > 0m)
            {
                int taxPayable = Account(cn, tx, "2100");
                Line(cn, tx, entryId, taxPayable, reversal ? tax : 0m, reversal ? 0m : tax);
            }
            // Snapshot-based provisional cost; not weighted-average valuation.
            decimal cost = SalesCostService.CostForInvoice(cn, tx, invoiceId, reversal);
            if (cost > 0m)
            {
                int cogs = Account(cn, tx, "5000");
                int inventory = Account(cn, tx, "1300");
                Line(cn, tx, entryId, cogs, reversal ? 0m : cost, reversal ? cost : 0m);
                Line(cn, tx, entryId, inventory, reversal ? cost : 0m, reversal ? 0m : cost);
            }
        }

        private static int Account(SqlConnection cn, SqlTransaction tx, string code)
        {
            using (var cmd = new SqlCommand("SELECT Id FROM dbo.Accounts WHERE Code=@code AND IsActive=1", cn, tx))
            {
                cmd.Parameters.Add("@code", SqlDbType.NVarChar, 30).Value = code;
                object result = cmd.ExecuteScalar();
                if (result == null) throw new InvalidOperationException("Missing active account " + code);
                return Convert.ToInt32(result);
            }
        }

        private static void Line(SqlConnection cn, SqlTransaction tx, int entry, int account, decimal debit, decimal credit)
        {
            using (var cmd = new SqlCommand(@"INSERT dbo.JournalLines(JournalEntryId,AccountId,Description,Debit,Credit)
VALUES(@entry,@account,N'Automatic sales posting',@debit,@credit)", cn, tx))
            {
                cmd.Parameters.Add("@entry", SqlDbType.Int).Value = entry;
                cmd.Parameters.Add("@account", SqlDbType.Int).Value = account;
                Money(cmd, "@debit", debit);
                Money(cmd, "@credit", credit);
                if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Journal line insert failed");
            }
        }

        private static void Money(SqlCommand cmd, string name, decimal value)
        {
            var parameter = cmd.Parameters.Add(name, SqlDbType.Decimal);
            parameter.Precision = 18;
            parameter.Scale = 2;
            parameter.Value = value;
        }
    }
}
