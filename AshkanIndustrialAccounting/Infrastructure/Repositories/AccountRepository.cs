using System.Collections.Generic;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Core.Interfaces;
using AshkanIndustrialAccounting.Core.Models;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.Infrastructure.Repositories
{
    public sealed class AccountRepository : IRepository<Account>
    {
        public IReadOnlyList<Account> GetAll()
        {
            var list = new List<Account>();
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand("SELECT Id,Code,NameEn,NameFa,Type,IsActive FROM Accounts ORDER BY Code", cn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(Map(r));
            return list;
        }
        public Account GetById(int id)
        {
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand("SELECT Id,Code,NameEn,NameFa,Type,IsActive FROM Accounts WHERE Id=@id", cn))
            { cmd.Parameters.AddWithValue("@id", id); using (var r=cmd.ExecuteReader()) return r.Read()?Map(r):null; }
        }
        public int Add(Account x)
        {
            AuthorizationService.RequireActiveSession("accounts");
            using (var cn=Db.Open())
            using (var cmd=new SqlCommand(@"INSERT INTO Accounts(Code,NameEn,NameFa,Type,IsActive) VALUES(@c,@en,@fa,@t,@a); SELECT CAST(SCOPE_IDENTITY() AS INT);",cn))
            { Bind(cmd,x); return (int)cmd.ExecuteScalar(); }
        }
        public void Update(Account x)
        {
            AuthorizationService.RequireActiveSession("accounts");
            using (var cn=Db.Open())
            using (var cmd=new SqlCommand(@"UPDATE Accounts SET Code=@c,NameEn=@en,NameFa=@fa,Type=@t,IsActive=@a WHERE Id=@id",cn))
            { Bind(cmd,x); cmd.Parameters.AddWithValue("@id",x.Id); cmd.ExecuteNonQuery(); }
        }
        public void Delete(int id)
        {
            AuthorizationService.RequireActiveSession("accounts");
            using(var cn=Db.Open()) using(var cmd=new SqlCommand("UPDATE Accounts SET IsActive=0 WHERE Id=@id",cn))
            { cmd.Parameters.AddWithValue("@id",id); cmd.ExecuteNonQuery(); }
        }
        private static void Bind(SqlCommand c, Account x)
        { c.Parameters.AddWithValue("@c",x.Code); c.Parameters.AddWithValue("@en",x.NameEn); c.Parameters.AddWithValue("@fa",x.NameFa??""); c.Parameters.AddWithValue("@t",x.Type); c.Parameters.AddWithValue("@a",x.IsActive); }
        private static Account Map(SqlDataReader r) => new Account { Id=r.GetInt32(0),Code=r.GetString(1),NameEn=r.GetString(2),NameFa=r.GetString(3),Type=r.GetString(4),IsActive=r.GetBoolean(5) };
    }
}
