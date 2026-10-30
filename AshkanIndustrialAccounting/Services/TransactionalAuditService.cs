using System;
using System.Data;
using System.Data.SqlClient;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Append-only audit writes in the caller's SQL transaction.</summary>
    public static class TransactionalAuditService
    {
        public static void Record(SqlConnection connection, SqlTransaction transaction, string user,
            string action, string entity, string entityId, string details)
        {
            if (connection == null || transaction == null) throw new ArgumentNullException("transaction");
            if (String.IsNullOrWhiteSpace(user) || user.Length > 100) throw new ArgumentException("Invalid user");
            if (String.IsNullOrWhiteSpace(action) || action.Length > 100) throw new ArgumentException("Invalid action");
            if (String.IsNullOrWhiteSpace(entity) || entity.Length > 100) throw new ArgumentException("Invalid entity");
            if ((entityId ?? "").Length > 100 || (details ?? "").Length > 4000)
                throw new ArgumentException("Audit field too long");
            using (var cmd = new SqlCommand(@"INSERT dbo.AuditLog
(UserName,ActionName,EntityName,EntityId,Details)
VALUES(@user,@action,@entity,@entityId,@details)", connection, transaction))
            {
                cmd.Parameters.Add("@user", SqlDbType.NVarChar,100).Value = user.Trim();
                cmd.Parameters.Add("@action", SqlDbType.NVarChar,100).Value = action;
                cmd.Parameters.Add("@entity", SqlDbType.NVarChar,100).Value = entity;
                cmd.Parameters.Add("@entityId", SqlDbType.NVarChar,100).Value = (object)entityId ?? DBNull.Value;
                cmd.Parameters.Add("@details", SqlDbType.NVarChar,4000).Value = (object)details ?? DBNull.Value;
                if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Audit write failed");
            }
        }
    }
}
