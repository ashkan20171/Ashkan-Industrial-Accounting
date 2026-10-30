using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Server-side filtered, read-only audit history.</summary>
    public static class AuditLogQueryService
    {
        public static DataTable Search(DateTime fromUtc, DateTime toUtcExclusive, string user, string action, int maxRows)
        {
            if (!AuthorizationService.CanAccess("auditlog"))
                throw new UnauthorizedAccessException("Audit history requires administrator access.");
            if (toUtcExclusive <= fromUtc) throw new ArgumentException("Invalid date range.");
            if (maxRows < 1 || maxRows > 2000) throw new ArgumentOutOfRangeException("maxRows");
            const string sql = @"SELECT TOP (@limit) Id,EventTime,UserName,ActionName,EntityName,EntityId,Details
FROM dbo.AuditLog
WHERE EventTime >= @from AND EventTime < @to
AND (@user=N'' OR UserName LIKE @user + N'%')
AND (@action=N'' OR ActionName LIKE @action + N'%')
ORDER BY EventTime DESC,Id DESC";
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(sql, cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@limit", SqlDbType.Int).Value = maxRows;
                cmd.Parameters.Add("@from", SqlDbType.DateTime2).Value = fromUtc;
                cmd.Parameters.Add("@to", SqlDbType.DateTime2).Value = toUtcExclusive;
                cmd.Parameters.Add("@user", SqlDbType.NVarChar, 100).Value = (user ?? "").Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                cmd.Parameters.Add("@action", SqlDbType.NVarChar, 100).Value = (action ?? "").Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                cmd.CommandTimeout = 30;
                var data = new DataTable();
                da.Fill(data);
                return data;
            }
        }
    }
}
