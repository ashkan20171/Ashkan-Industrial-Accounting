using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    public static class AuditService
    {
        public static void TryWrite(string action, string entity = null, string details = null)
        {
            if (string.IsNullOrWhiteSpace(action)) return;
            try
            {
                using (var cn = Db.Open())
                using (var cmd = new SqlCommand(@"INSERT dbo.AuditLog(UserName, ActionName, EntityName, Details) VALUES(@user,@action,@entity,@details)", cn))
                {
                    cmd.Parameters.Add("@user", SqlDbType.NVarChar, 100).Value = AuthorizationService.CurrentUserName;
                    cmd.Parameters.Add("@action", SqlDbType.NVarChar, 100).Value = action;
                    cmd.Parameters.Add("@entity", SqlDbType.NVarChar, 100).Value = (object)entity ?? DBNull.Value;
                    cmd.Parameters.Add("@details", SqlDbType.NVarChar, -1).Value = (object)details ?? DBNull.Value;
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* Audit logging must not crash the accounting UI; surface DB health separately. */ }
        }
    }
}
