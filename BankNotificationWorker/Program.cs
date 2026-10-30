using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace Ashkan.BankNotificationWorker
{
    // Independently deployable background worker. The outbox is written in the
    // accounting transaction; this process performs idempotent inbox delivery.
    internal static class Program
    {
        private static volatile bool stopping;
        private static void Main()
        {
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; stopping = true; };
            int interval = ReadSetting("PollIntervalSeconds", 15, 2, 3600);
            int batch = ReadSetting("BatchSize", 25, 1, 250);
            Console.WriteLine("Ashkan Bank Notification Worker started. Ctrl+C to stop.");
            while (!stopping)
            {
                string cycleError = null;
                try { int delivered = ProcessBatch(batch); if (delivered > 0) Console.WriteLine("Delivered notifications: " + delivered); }
                catch (Exception ex) { cycleError = ex.Message; Console.Error.WriteLine(DateTime.UtcNow.ToString("o") + " " + ex.Message); }
                WriteHeartbeat(cycleError);
                for (int i = 0; i < interval * 10 && !stopping; i++) Thread.Sleep(100);
            }
        }

        private static int ProcessBatch(int batchSize)
        {
            int delivered = 0;
            using (var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["AccountingDb"].ConnectionString))
            {
                cn.Open();
                for (int i = 0; i < batchSize && !stopping; i++)
                {
                    using (var tx = cn.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        long outboxId; int alertId; int? userId; string severity; string recommendation; bool enabled;
                        const string take = @"SELECT TOP(1) o.Id,o.AlertId,a.AssignedToUserId,a.SeverityCode,a.Recommendation,
CASE WHEN p.UserId IS NULL THEN CAST(1 AS bit)
 WHEN p.EnableInApp=0 THEN CAST(0 AS bit)
 WHEN UPPER(a.SeverityCode)='CRITICAL' AND p.NotifyCritical=0 THEN CAST(0 AS bit)
 WHEN UPPER(a.SeverityCode)='WARNING' AND p.NotifyHigh=0 THEN CAST(0 AS bit)
 WHEN UPPER(a.SeverityCode)='INFORMATION' AND p.NotifyMedium=0 THEN CAST(0 AS bit)
 WHEN UPPER(a.SeverityCode)='LOW' AND p.NotifyLow=0 THEN CAST(0 AS bit)
 ELSE CAST(1 AS bit) END
FROM dbo.BankAlertNotificationOutbox o WITH(UPDLOCK,READPAST,ROWLOCK)
INNER JOIN dbo.BankImportAlerts a ON a.Id=o.AlertId
LEFT JOIN dbo.UserNotificationPreferences p ON p.UserId=a.AssignedToUserId
WHERE o.ProcessedAtUtc IS NULL AND a.AssignedToUserId IS NOT NULL
ORDER BY o.Id;";
                        using (var cmd = new SqlCommand(take, cn, tx))
                        using (var rd = cmd.ExecuteReader())
                        {
                            if (!rd.Read()) { tx.Commit(); break; }
                            outboxId = rd.GetInt64(0); alertId = rd.GetInt32(1);
                            userId = rd.IsDBNull(2) ? (int?)null : rd.GetInt32(2);
                            severity = rd.GetString(3); recommendation = rd.GetString(4); enabled = rd.GetBoolean(5);
                        }
                        try
                        {
                            if (!enabled)
                            {
                                using (var skip = new SqlCommand("UPDATE dbo.BankAlertNotificationOutbox SET ProcessedAtUtc=SYSUTCDATETIME(),AttemptCount=AttemptCount+1,LastError=NULL WHERE Id=@id", cn, tx))
                                { skip.Parameters.Add("@id", SqlDbType.BigInt).Value = outboxId; skip.ExecuteNonQuery(); }
                                tx.Commit();
                                continue;
                            }
                            using (var add = new SqlCommand(@"IF NOT EXISTS(SELECT 1 FROM dbo.UserNotifications WHERE OutboxId=@outbox AND UserId=@user)
INSERT dbo.UserNotifications(UserId,AlertId,OutboxId,Title,Message,SeverityCode)
VALUES(@user,@alert,@outbox,@title,@message,@severity);
INSERT dbo.EmailNotificationOutbox(UserId,NotificationId,RecipientEmail,Subject,Body)
SELECT u.UserId,n.Id,u.Email,N'هشدار بانکی جدید',n.Message
FROM dbo.UserNotifications n
INNER JOIN dbo.AppUsers u ON u.UserId=n.UserId
LEFT JOIN dbo.UserNotificationPreferences p ON p.UserId=u.UserId
WHERE n.OutboxId=@outbox AND n.UserId=@user AND NULLIF(LTRIM(RTRIM(u.Email)),N'') IS NOT NULL
 AND ISNULL(p.EnableEmail,0)=1
 AND NOT EXISTS(SELECT 1 FROM dbo.EmailNotificationOutbox e WHERE e.NotificationId=n.Id);
UPDATE dbo.BankAlertNotificationOutbox SET ProcessedAtUtc=SYSUTCDATETIME(),AttemptCount=AttemptCount+1,LastError=NULL WHERE Id=@outbox;", cn, tx))
                            {
                                add.Parameters.Add("@outbox", SqlDbType.BigInt).Value = outboxId;
                                add.Parameters.Add("@user", SqlDbType.Int).Value = userId.Value;
                                add.Parameters.Add("@alert", SqlDbType.Int).Value = alertId;
                                add.Parameters.Add("@title", SqlDbType.NVarChar, 200).Value = "هشدار بانکی جدید";
                                add.Parameters.Add("@message", SqlDbType.NVarChar, 1000).Value = "هشدار " + severity + " برای پیگیری ثبت شد. " + recommendation;
                                add.Parameters.Add("@severity", SqlDbType.NVarChar, 20).Value = severity;
                                add.ExecuteNonQuery();
                            }
                            tx.Commit(); delivered++;
                        }
                        catch (Exception ex)
                        {
                            using (var fail = new SqlCommand("UPDATE dbo.BankAlertNotificationOutbox SET AttemptCount=AttemptCount+1,LastError=@error WHERE Id=@id", cn, tx))
                            { fail.Parameters.Add("@error", SqlDbType.NVarChar, 1000).Value = ex.Message.Length > 1000 ? ex.Message.Substring(0, 1000) : ex.Message; fail.Parameters.Add("@id", SqlDbType.BigInt).Value = outboxId; fail.ExecuteNonQuery(); }
                            tx.Commit(); throw;
                        }
                    }
                }
            }
            return delivered;
        }

        private static int ReadSetting(string key, int fallback, int min, int max)
        {
            int value; return Int32.TryParse(ConfigurationManager.AppSettings[key], out value) && value >= min && value <= max ? value : fallback;
        }

        private static void WriteHeartbeat(string error)
        {
            try
            {
                using (var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["AccountingDb"].ConnectionString))
                using (var cmd = new SqlCommand(@"IF EXISTS(SELECT 1 FROM dbo.BackgroundWorkerStatus WHERE WorkerName=@name)
UPDATE dbo.BackgroundWorkerStatus SET LastHeartbeatUtc=SYSUTCDATETIME(),LastStatus=CASE WHEN @error IS NULL THEN N'Healthy' ELSE N'Error' END,LastError=@error,UpdatedAtUtc=SYSUTCDATETIME() WHERE WorkerName=@name;
ELSE INSERT dbo.BackgroundWorkerStatus(WorkerName,LastHeartbeatUtc,LastStatus,LastError) VALUES(@name,SYSUTCDATETIME(),CASE WHEN @error IS NULL THEN N'Healthy' ELSE N'Error' END,@error);
INSERT dbo.BackgroundWorkerHistory(WorkerName,HeartbeatUtc,Status,ErrorMessage) VALUES(@name,SYSUTCDATETIME(),CASE WHEN @error IS NULL THEN N'Healthy' ELSE N'Error' END,@error);", cn))
                {
                    cmd.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = "BankNotificationWorker";
                    cmd.Parameters.Add("@error", SqlDbType.NVarChar, 1000).Value = String.IsNullOrWhiteSpace(error) ? (object)DBNull.Value : error.Substring(0, Math.Min(1000, error.Length));
                    cn.Open(); cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { Console.Error.WriteLine("Heartbeat write failed: " + ex.Message); }
        }
    }
}
