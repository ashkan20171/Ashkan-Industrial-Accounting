using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Atomic single-currency cash receipts and payments with double-entry journals.</summary>
    public static class CashBankTransactionService
    {
        public static int Post(int cashAccountId, string direction, decimal amount, string reference, string description)
        {
            return Post(cashAccountId, direction, amount, reference, description, null, null);
        }

        public static int Post(int cashAccountId, string direction, decimal amount, string reference,
            string description, int? offsetLedgerId, int? partnerId)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (cashAccountId <= 0 || (direction != "Receipt" && direction != "Payment") ||
                amount <= 0m || decimal.Round(amount, 2) != amount)
                throw new ArgumentException("Invalid cash/bank transaction.");
            if ((reference ?? "").Length > 80 || (description ?? "").Length > 500)
                throw new ArgumentException("Reference or description is too long.");
            if (offsetLedgerId.HasValue && offsetLedgerId.Value <= 0)
                throw new ArgumentException("Invalid offset account.");
            if (partnerId.HasValue && partnerId.Value <= 0)
                throw new ArgumentException("Invalid business partner.");
            using (var cn = Db.Open())
            using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    string type, currency;
                    decimal balance;
                    using (var cmd = new SqlCommand(@"SELECT AccountType,CurrencyCode,Balance FROM dbo.CashBankAccounts WITH(UPDLOCK,HOLDLOCK)
WHERE Id=@id AND IsActive=1", cn, tx))
                    {
                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = cashAccountId;
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) throw new InvalidOperationException("Cash/bank account not found or inactive.");
                            type = r.GetString(0); currency = r.GetString(1); balance = r.GetDecimal(2);
                        }
                    }
                    if (currency != "IRR") throw new InvalidOperationException("This posting flow supports IRR only; FX requires a separate policy.");
                    if (type != "Cash" && type != "Bank") throw new InvalidOperationException("Unsupported account type.");
                    if (direction == "Payment" && balance < amount)
                        throw new InvalidOperationException("Insufficient account balance.");
                    string cashCode = type == "Cash" ? "1000" : "1100";
                    string offsetCode = direction == "Receipt" ? "4000" : "5300";
                    int cashLedger = GetLedger(cn, tx, cashCode);
                    int offsetLedger = offsetLedgerId.HasValue ?
                        GetLedgerById(cn, tx, offsetLedgerId.Value) : GetLedger(cn, tx, offsetCode);
                    if (cashLedger == offsetLedger)
                        throw new InvalidOperationException("Cash account and offset ledger cannot be the same.");
                    if (partnerId.HasValue) ValidatePartner(cn, tx, partnerId.Value);
                    int paymentId;
                    using (var cmd = new SqlCommand(@"INSERT dbo.Payments(PaymentNo,PaymentDate,Direction,PartnerId,CashBankAccountId,Amount,CurrencyCode,ReferenceNo,Description)
VALUES(@no,SYSUTCDATETIME(),@direction,@partner,@account,@amount,@currency,@reference,@description);
SELECT CAST(SCOPE_IDENTITY() AS int)", cn, tx))
                    {
                        cmd.Parameters.Add("@no", SqlDbType.NVarChar, 40).Value = "CB-" + Guid.NewGuid().ToString("N");
                        cmd.Parameters.Add("@direction", SqlDbType.NVarChar, 20).Value = direction;
                        cmd.Parameters.Add("@partner", SqlDbType.Int).Value =
                            partnerId.HasValue ? (object)partnerId.Value : DBNull.Value;
                        cmd.Parameters.Add("@account", SqlDbType.Int).Value = cashAccountId;
                        var a=cmd.Parameters.Add("@amount", SqlDbType.Decimal); a.Precision=18; a.Scale=2; a.Value=amount;
                        cmd.Parameters.Add("@currency", SqlDbType.NVarChar, 10).Value = currency;
                        cmd.Parameters.Add("@reference", SqlDbType.NVarChar, 80).Value = (object)reference ?? DBNull.Value;
                        cmd.Parameters.Add("@description", SqlDbType.NVarChar, 500).Value = (object)description ?? DBNull.Value;
                        paymentId=(int)cmd.ExecuteScalar();
                    }
                    using (var cmd = new SqlCommand(@"UPDATE dbo.CashBankAccounts SET Balance=Balance+@delta WHERE Id=@id", cn, tx))
                    {
                        var delta=cmd.Parameters.Add("@delta", SqlDbType.Decimal);delta.Precision=18;delta.Scale=2;
                        delta.Value=direction=="Receipt"?amount:-amount;
                        cmd.Parameters.Add("@id", SqlDbType.Int).Value=cashAccountId;
                        if (cmd.ExecuteNonQuery()!=1) throw new InvalidOperationException("Balance update failed.");
                    }
                    int journalId;
                    using (var cmd = new SqlCommand(@"INSERT dbo.JournalEntries(EntryNo,EntryDate,Description,ReferenceNo,IsPosted)
VALUES(@no,SYSUTCDATETIME(),@desc,@reference,1);SELECT CAST(SCOPE_IDENTITY() AS int)",cn,tx))
                    {
                        cmd.Parameters.Add("@no",SqlDbType.NVarChar,40).Value="CBJ-"+paymentId;
                        cmd.Parameters.Add("@desc",SqlDbType.NVarChar,500).Value=description??"Cash/bank posting";
                        cmd.Parameters.Add("@reference",SqlDbType.NVarChar,80).Value="CB-"+paymentId;
                        journalId=(int)cmd.ExecuteScalar();
                    }
                    bool receipt=direction=="Receipt";
                    InsertLine(cn,tx,journalId,cashLedger,receipt?amount:0m,receipt?0m:amount);
                    InsertLine(cn,tx,journalId,offsetLedger,receipt?0m:amount,receipt?amount:0m);
                    TransactionalAuditService.Record(cn,tx,AuthorizationService.CurrentUserName,
                        "CashBank"+direction,"Payment",paymentId.ToString(),
                        "CashBankAccount="+cashAccountId+";Amount="+amount+";Currency="+currency);
                    tx.Commit();
                    return paymentId;
                }
                catch { tx.Rollback(); throw; }
            }
        }
        private static int GetLedgerById(SqlConnection cn, SqlTransaction tx, int id)
        {
            using (var cmd = new SqlCommand(
                "SELECT Id FROM dbo.Accounts WHERE Id=@id AND IsActive=1", cn, tx))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                var result = cmd.ExecuteScalar();
                if (result == null) throw new InvalidOperationException("Offset ledger is missing or inactive.");
                return Convert.ToInt32(result);
            }
        }
        private static void ValidatePartner(SqlConnection cn, SqlTransaction tx, int id)
        {
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.BusinessPartners WHERE Id=@id",cn,tx))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value=id;
                if (Convert.ToInt32(cmd.ExecuteScalar())!=1)
                    throw new InvalidOperationException("Business partner not found.");
            }
        }

        private static int GetLedger(SqlConnection cn,SqlTransaction tx,string code)
        {
            using(var cmd=new SqlCommand("SELECT Id FROM dbo.Accounts WHERE Code=@code AND IsActive=1",cn,tx))
            {
                cmd.Parameters.Add("@code",SqlDbType.NVarChar,30).Value=code;
                var id=cmd.ExecuteScalar();
                if(id==null) throw new InvalidOperationException("Required ledger account missing: "+code);
                return Convert.ToInt32(id);
            }
        }
        private static void InsertLine(SqlConnection cn,SqlTransaction tx,int journalId,int accountId,decimal debit,decimal credit)
        {
            using(var cmd=new SqlCommand(@"INSERT dbo.JournalLines(JournalEntryId,AccountId,Description,Debit,Credit)
VALUES(@j,@a,N'Cash/bank posting',@debit,@credit)",cn,tx))
            {
                cmd.Parameters.Add("@j",SqlDbType.Int).Value=journalId;
                cmd.Parameters.Add("@a",SqlDbType.Int).Value=accountId;
                var d=cmd.Parameters.Add("@debit",SqlDbType.Decimal);d.Precision=18;d.Scale=2;d.Value=debit;
                var c=cmd.Parameters.Add("@credit",SqlDbType.Decimal);c.Precision=18;c.Scale=2;c.Value=credit;
                if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Journal line insert failed.");
            }
        }
    }
}
