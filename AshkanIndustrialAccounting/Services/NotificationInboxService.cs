using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Application-facing inbox adapter; delivery remains owned by the independent worker.</summary>
    public static class NotificationInboxService
    {
        private static int ResolveCurrentUserId(SqlConnection cn)
        {
            using (var cmd = new SqlCommand("SELECT UserId FROM dbo.AppUsers WHERE UserName=@name AND IsActive=1", cn))
            {
                cmd.Parameters.Add("@name", SqlDbType.NVarChar, 80).Value = AuthorizationService.CurrentUserName;
                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value) throw new UnauthorizedAccessException("Active user was not found.");
                return Convert.ToInt32(value);
            }
        }

        public static int GetUnreadCount()
        {
            using (var cn = Db.Open())
            {
                int userId = ResolveCurrentUserId(cn);
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.UserNotifications WHERE UserId=@user AND ReadAtUtc IS NULL", cn))
                { cmd.Parameters.Add("@user", SqlDbType.Int).Value = userId; return Convert.ToInt32(cmd.ExecuteScalar()); }
            }
        }

        public static DataTable GetInbox(bool unreadOnly)
        {
            using (var cn = Db.Open())
            {
                int userId = ResolveCurrentUserId(cn);
                using (var cmd = new SqlCommand(@"SELECT Id,AlertId,Title,Message,SeverityCode,CreatedAtUtc,ReadAtUtc
FROM dbo.UserNotifications WHERE UserId=@user AND (@unread=0 OR ReadAtUtc IS NULL)
ORDER BY CASE WHEN ReadAtUtc IS NULL THEN 0 ELSE 1 END,CreatedAtUtc DESC,Id DESC;", cn))
                {
                    cmd.Parameters.Add("@user", SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@unread", SqlDbType.Bit).Value = unreadOnly;
                    var table = new DataTable(); using (var adapter = new SqlDataAdapter(cmd)) adapter.Fill(table); return table;
                }
            }
        }

        public static bool MarkAsRead(long notificationId)
        {
            using (var cn = Db.Open())
            {
                int userId = ResolveCurrentUserId(cn);
                using (var cmd = new SqlCommand("UPDATE dbo.UserNotifications SET ReadAtUtc=SYSUTCDATETIME() WHERE Id=@id AND UserId=@user AND ReadAtUtc IS NULL", cn))
                { cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = notificationId; cmd.Parameters.Add("@user", SqlDbType.Int).Value = userId; return cmd.ExecuteNonQuery() == 1; }
            }
        }


        public sealed class NotificationPreferences
        {
            public bool EnableInApp { get; set; } = true;
            public bool NotifyCritical { get; set; } = true;
            public bool NotifyHigh { get; set; } = true;
            public bool NotifyMedium { get; set; } = true;
            public bool NotifyLow { get; set; } = true;
        }

        public static NotificationPreferences GetPreferences()
        {
            using (var cn = Db.Open())
            {
                int userId = ResolveCurrentUserId(cn);
                using (var cmd = new SqlCommand(@"SELECT EnableInApp,NotifyCritical,NotifyHigh,NotifyMedium,NotifyLow
FROM dbo.UserNotificationPreferences WHERE UserId=@user;", cn))
                {
                    cmd.Parameters.Add("@user", SqlDbType.Int).Value = userId;
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read()) return new NotificationPreferences();
                        return new NotificationPreferences { EnableInApp = rd.GetBoolean(0), NotifyCritical = rd.GetBoolean(1),
                            NotifyHigh = rd.GetBoolean(2), NotifyMedium = rd.GetBoolean(3), NotifyLow = rd.GetBoolean(4) };
                    }
                }
            }
        }

        public static void SavePreferences(NotificationPreferences preferences)
        {
            if (preferences == null) throw new ArgumentNullException(nameof(preferences));
            using (var cn = Db.Open())
            {
                int userId = ResolveCurrentUserId(cn);
                using (var cmd = new SqlCommand(@"UPDATE dbo.UserNotificationPreferences SET EnableInApp=@enabled,NotifyCritical=@critical,
NotifyHigh=@high,NotifyMedium=@medium,NotifyLow=@low,UpdatedAtUtc=SYSUTCDATETIME() WHERE UserId=@user;
IF @@ROWCOUNT=0 INSERT dbo.UserNotificationPreferences(UserId,EnableInApp,NotifyCritical,NotifyHigh,NotifyMedium,NotifyLow)
VALUES(@user,@enabled,@critical,@high,@medium,@low);", cn))
                {
                    cmd.Parameters.Add("@user", SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@enabled", SqlDbType.Bit).Value = preferences.EnableInApp;
                    cmd.Parameters.Add("@critical", SqlDbType.Bit).Value = preferences.NotifyCritical;
                    cmd.Parameters.Add("@high", SqlDbType.Bit).Value = preferences.NotifyHigh;
                    cmd.Parameters.Add("@medium", SqlDbType.Bit).Value = preferences.NotifyMedium;
                    cmd.Parameters.Add("@low", SqlDbType.Bit).Value = preferences.NotifyLow;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static int MarkAllAsRead()
        {
            using (var cn = Db.Open())
            {
                int userId = ResolveCurrentUserId(cn);
                using (var cmd = new SqlCommand("UPDATE dbo.UserNotifications SET ReadAtUtc=SYSUTCDATETIME() WHERE UserId=@user AND ReadAtUtc IS NULL", cn))
                { cmd.Parameters.Add("@user", SqlDbType.Int).Value = userId; return cmd.ExecuteNonQuery(); }
            }
        }
    }
}
