using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace Ashkan.EmailNotificationWorker
{
    internal static class Program
    {
        private static volatile bool stopping;
        private sealed class EmailJob { public long Id; public string To; public string Subject; public string Body; public int Attempts; }
        private static void Main()
        {
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; stopping = true; };
            int interval = ReadInt("PollIntervalSeconds", 20, 2, 3600);
            int batch = ReadInt("BatchSize", 10, 1, 100);
            Console.WriteLine("Ashkan Email Notification Worker started. Ctrl+C to stop.");
            while (!stopping)
            {
                string cycleError = null;
                try { for (int i = 0; i < batch && !stopping; i++) { var job = Take(); if (job == null) break; Send(job); } }
                catch (Exception ex) { cycleError = ex.Message; Console.Error.WriteLine(DateTime.UtcNow.ToString("o") + " " + ex.Message); }
                WriteHeartbeat(cycleError);
                for (int i = 0; i < interval * 10 && !stopping; i++) Thread.Sleep(100);
            }
        }
        private static EmailJob Take()
        {
            using (var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["AccountingDb"].ConnectionString))
            {
                cn.Open(); using (var tx = cn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    const string sql = @";WITH candidate AS (SELECT TOP(1) * FROM dbo.EmailNotificationOutbox WITH(UPDLOCK,READPAST,ROWLOCK)
WHERE Status=N'Pending' AND NextAttemptAtUtc<=SYSUTCDATETIME() ORDER BY Id)
UPDATE candidate SET AttemptCount=AttemptCount+1 OUTPUT inserted.Id,inserted.RecipientEmail,inserted.Subject,inserted.Body,inserted.AttemptCount;";
                    EmailJob job = null;
                    using (var cmd = new SqlCommand(sql, cn, tx)) using (var rd = cmd.ExecuteReader())
                        if (rd.Read()) job = new EmailJob { Id = rd.GetInt64(0), To = rd.GetString(1), Subject = rd.GetString(2), Body = rd.GetString(3), Attempts = rd.GetInt32(4) };
                    tx.Commit(); return job;
                }
            }
        }
        private static void Send(EmailJob job)
        {
            try
            {
                string host = Required("SmtpHost"); int port = ReadInt("SmtpPort", 587, 1, 65535);
                string from = Required("SmtpFrom"); string user = ConfigurationManager.AppSettings["SmtpUser"] ?? "";
                string password = ConfigurationManager.AppSettings["SmtpPassword"] ?? "";
                using (var message = new MailMessage(from, job.To, job.Subject, job.Body))
                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = String.Equals(ConfigurationManager.AppSettings["SmtpEnableSsl"], "true", StringComparison.OrdinalIgnoreCase);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network; client.Timeout = 30000;
                    if (!String.IsNullOrWhiteSpace(user)) client.Credentials = new NetworkCredential(user, password);
                    client.Send(message);
                }
                Update(job.Id, true, null, 0);
                Console.WriteLine("Email sent. QueueId=" + job.Id);
            }
            catch (Exception ex)
            {
                int max = ReadInt("MaxAttempts", 8, 1, 50);
                int delay = Math.Min(360, (int)Math.Pow(2, Math.Min(job.Attempts, 8)));
                Update(job.Id, false, ex.Message, max, delay);
                Console.Error.WriteLine("Email failed. QueueId=" + job.Id + " " + ex.Message);
            }
        }
        private static void Update(long id, bool sent, string error, int maxAttempts, int delayMinutes = 0)
        {
            using (var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["AccountingDb"].ConnectionString))
            using (var cmd = new SqlCommand(@"UPDATE dbo.EmailNotificationOutbox SET
Status=CASE WHEN @sent=1 THEN N'Sent' WHEN AttemptCount>=@max THEN N'DeadLetter' ELSE N'Pending' END,
SentAtUtc=CASE WHEN @sent=1 THEN SYSUTCDATETIME() ELSE SentAtUtc END,
NextAttemptAtUtc=CASE WHEN @sent=1 OR AttemptCount>=@max THEN NextAttemptAtUtc ELSE DATEADD(MINUTE,@delay,SYSUTCDATETIME()) END,
LastError=@error WHERE Id=@id", cn))
            {
                cmd.Parameters.Add("@sent", SqlDbType.Bit).Value = sent; cmd.Parameters.Add("@max", SqlDbType.Int).Value = maxAttempts;
                cmd.Parameters.Add("@delay", SqlDbType.Int).Value = delayMinutes; cmd.Parameters.Add("@error", SqlDbType.NVarChar, 1000).Value = (object)(error == null ? null : error.Substring(0, Math.Min(1000, error.Length))) ?? DBNull.Value;
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = id; cn.Open(); cmd.ExecuteNonQuery();
            }
        }
        private static string Required(string key) { string value = ConfigurationManager.AppSettings[key]; if (String.IsNullOrWhiteSpace(value)) throw new ConfigurationErrorsException("Missing appSetting: " + key); return value; }
        private static int ReadInt(string key, int fallback, int min, int max) { int value; return Int32.TryParse(ConfigurationManager.AppSettings[key], out value) && value >= min && value <= max ? value : fallback; }

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
                    cmd.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = "EmailNotificationWorker";
                    cmd.Parameters.Add("@error", SqlDbType.NVarChar, 1000).Value = String.IsNullOrWhiteSpace(error) ? (object)DBNull.Value : error.Substring(0, Math.Min(1000, error.Length));
                    cn.Open(); cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { Console.Error.WriteLine("Heartbeat write failed: " + ex.Message); }
        }
    }
}
