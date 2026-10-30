using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Core.Models;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.Infrastructure.Repositories
{
    public sealed class JournalRepository
    {
        public int Save(JournalEntry entry)
        {
            AuthorizationService.RequireActiveSession("journal");
            using (var cn=Db.Open())
            using (var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    int id;
                    using(var cmd=new SqlCommand(@"INSERT INTO JournalEntries(EntryNo,EntryDate,Description,ReferenceNo,IsPosted)
VALUES(@no,@date,@desc,@ref,@posted); SELECT CAST(SCOPE_IDENTITY() AS INT);",cn,tx))
                    {
                        cmd.Parameters.AddWithValue("@no",entry.EntryNo); cmd.Parameters.AddWithValue("@date",entry.EntryDate);
                        cmd.Parameters.AddWithValue("@desc",entry.Description??""); cmd.Parameters.AddWithValue("@ref",entry.ReferenceNo??"");
                        cmd.Parameters.AddWithValue("@posted",entry.IsPosted); id=(int)cmd.ExecuteScalar();
                    }
                    foreach(var l in entry.Lines)
                    using(var cmd=new SqlCommand(@"INSERT INTO JournalLines(JournalEntryId,AccountId,Description,Debit,Credit,CostCenterId)
VALUES(@j,@a,@d,@dr,@cr,@cc)",cn,tx))
                    {
                        cmd.Parameters.AddWithValue("@j",id); cmd.Parameters.AddWithValue("@a",l.AccountId);
                        cmd.Parameters.AddWithValue("@d",l.Description??""); cmd.Parameters.AddWithValue("@dr",l.Debit);
                        cmd.Parameters.AddWithValue("@cr",l.Credit); cmd.Parameters.AddWithValue("@cc",(object)l.CostCenterId??DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                    TransactionalAuditService.Record(cn, tx, AuthorizationService.CurrentUserName,
                        "JournalPosted", "JournalEntry", id.ToString(), "EntryNo=" + entry.EntryNo);
                    tx.Commit(); return id;
                } catch { tx.Rollback(); throw; }
            }
        }
    }
}
