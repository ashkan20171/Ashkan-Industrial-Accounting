using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Atomic IRR transfer between two distinct cash/bank accounts.</summary>
    public static class CashBankTransferService
    {
        public static int Transfer(int sourceId, int destinationId, decimal amount, string requestKey, string note)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (sourceId <= 0 || destinationId <= 0 || sourceId == destinationId ||
                amount <= 0m || decimal.Round(amount, 2) != amount)
                throw new ArgumentException("Invalid transfer accounts or amount.");
            if (String.IsNullOrWhiteSpace(requestKey) || requestKey.Length > 80 || (note ?? "").Length > 500)
                throw new ArgumentException("A unique request key (up to 80 characters) is required.");
            requestKey = requestKey.Trim();
            using (var cn = Db.Open())
            using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    // Lock accounts in deterministic order to reduce deadlocks on opposite transfers.
                    int first = Math.Min(sourceId, destinationId), second = Math.Max(sourceId, destinationId);
                    var firstInfo = ReadAccount(cn, tx, first);
                    var secondInfo = ReadAccount(cn, tx, second);
                    var source = sourceId == first ? firstInfo : secondInfo;
                    var destination = destinationId == first ? firstInfo : secondInfo;
                    if (source.Currency != "IRR" || destination.Currency != "IRR")
                        throw new InvalidOperationException("Only IRR transfers are supported.");
                    if (source.Balance < amount) throw new InvalidOperationException("Insufficient source balance.");
                    int transferId;
                    using (var cmd = new SqlCommand(@"INSERT dbo.CashBankTransfers
(RequestKey,SourceAccountId,DestinationAccountId,Amount,CurrencyCode,Note,CreatedBy)
VALUES(@key,@source,@destination,@amount,N'IRR',@note,@user);
SELECT CAST(SCOPE_IDENTITY() AS int)", cn, tx))
                    {
                        cmd.Parameters.Add("@key",SqlDbType.NVarChar,80).Value=requestKey;
                        cmd.Parameters.Add("@source",SqlDbType.Int).Value=sourceId;
                        cmd.Parameters.Add("@destination",SqlDbType.Int).Value=destinationId;
                        var a=cmd.Parameters.Add("@amount",SqlDbType.Decimal);a.Precision=18;a.Scale=2;a.Value=amount;
                        cmd.Parameters.Add("@note",SqlDbType.NVarChar,500).Value=(object)note??DBNull.Value;
                        cmd.Parameters.Add("@user",SqlDbType.NVarChar,100).Value=AuthorizationService.CurrentUserName;
                        transferId=(int)cmd.ExecuteScalar();
                    }
                    UpdateBalance(cn,tx,sourceId,-amount);
                    UpdateBalance(cn,tx,destinationId,amount);
                    int journalId;
                    using (var cmd = new SqlCommand(@"INSERT dbo.JournalEntries(EntryNo,EntryDate,Description,ReferenceNo,IsPosted)
VALUES(@no,SYSUTCDATETIME(),@note,@ref,1); SELECT CAST(SCOPE_IDENTITY() AS int)",cn,tx))
                    {
                        cmd.Parameters.Add("@no",SqlDbType.NVarChar,40).Value="TRJ-"+transferId;
                        cmd.Parameters.Add("@note",SqlDbType.NVarChar,500).Value=(object)note??"Cash/bank transfer";
                        cmd.Parameters.Add("@ref",SqlDbType.NVarChar,80).Value="TR-"+transferId;
                        journalId=(int)cmd.ExecuteScalar();
                    }
                    InsertLine(cn,tx,journalId,source.LedgerId,0m,amount);
                    InsertLine(cn,tx,journalId,destination.LedgerId,amount,0m);
                    TransactionalAuditService.Record(cn,tx,AuthorizationService.CurrentUserName,
                        "CashBankTransfer","CashBankTransfer",transferId.ToString(),
                        "From="+sourceId+";To="+destinationId+";Amount="+amount);
                    tx.Commit();
                    return transferId;
                }
                catch { tx.Rollback(); throw; }
            }
        }
        private sealed class AccountInfo
        {
            public decimal Balance; public string Currency; public int LedgerId;
        }
        private static AccountInfo ReadAccount(SqlConnection cn,SqlTransaction tx,int id)
        {
            using (var cmd = new SqlCommand(@"SELECT AccountType,CurrencyCode,Balance
FROM dbo.CashBankAccounts WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id AND IsActive=1",cn,tx))
            {
                cmd.Parameters.Add("@id",SqlDbType.Int).Value=id;
                using (var r=cmd.ExecuteReader())
                {
                    if (!r.Read()) throw new InvalidOperationException("Cash/bank account missing or inactive.");
                    string type=r.GetString(0), currency=r.GetString(1);decimal balance=r.GetDecimal(2);
                    if (type!="Cash" && type!="Bank") throw new InvalidOperationException("Unsupported account type.");
                    r.Close();
                    string code=type=="Cash"?"1000":"1100";
                    using(var ledger=new SqlCommand("SELECT Id FROM dbo.Accounts WHERE Code=@code AND IsActive=1",cn,tx))
                    {
                        ledger.Parameters.Add("@code",SqlDbType.NVarChar,30).Value=code;
                        var found=ledger.ExecuteScalar();
                        if(found==null)throw new InvalidOperationException("Missing ledger for cash/bank account.");
                        return new AccountInfo{Balance=balance,Currency=currency,LedgerId=Convert.ToInt32(found)};
                    }
                }
            }
        }
        private static void UpdateBalance(SqlConnection cn,SqlTransaction tx,int id,decimal delta)
        {
            using(var cmd=new SqlCommand("UPDATE dbo.CashBankAccounts SET Balance=Balance+@delta WHERE Id=@id",cn,tx))
            {
                cmd.Parameters.Add("@id",SqlDbType.Int).Value=id;
                var a=cmd.Parameters.Add("@delta",SqlDbType.Decimal);a.Precision=18;a.Scale=2;a.Value=delta;
                if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Balance update failed.");
            }
        }
        private static void InsertLine(SqlConnection cn,SqlTransaction tx,int journal,int ledger,decimal debit,decimal credit)
        {
            using(var cmd=new SqlCommand(@"INSERT dbo.JournalLines(JournalEntryId,AccountId,Description,Debit,Credit)
VALUES(@journal,@ledger,N'Cash/bank transfer',@debit,@credit)",cn,tx))
            {
                cmd.Parameters.Add("@journal",SqlDbType.Int).Value=journal;
                cmd.Parameters.Add("@ledger",SqlDbType.Int).Value=ledger;
                var d=cmd.Parameters.Add("@debit",SqlDbType.Decimal);d.Precision=18;d.Scale=2;d.Value=debit;
                var c=cmd.Parameters.Add("@credit",SqlDbType.Decimal);c.Precision=18;c.Scale=2;c.Value=credit;
                if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Transfer journal line failed.");
            }
        }
    }
}
