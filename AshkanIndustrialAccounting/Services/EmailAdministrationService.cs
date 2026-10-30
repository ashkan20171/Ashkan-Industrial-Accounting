using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    public static class EmailAdministrationService
    {
        private static void RequireAdmin() { AuthorizationService.RequireActiveSession("users"); }
        public static void UpdateUserEmail(int userId, string email)
        {
            RequireAdmin();
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
            email = (email ?? String.Empty).Trim();
            if (email.Length > 254) throw new ArgumentException("Email cannot exceed 254 characters.");
            if (email.Length > 0 && !new EmailAddressAttribute().IsValid(email)) throw new ArgumentException("آدرس ایمیل معتبر نیست.");
            using (var cn = Db.Open()) using (var cmd = new SqlCommand("UPDATE dbo.AppUsers SET Email=@email WHERE UserId=@id", cn))
            { cmd.Parameters.Add("@email", SqlDbType.NVarChar, 254).Value = email.Length == 0 ? (object)DBNull.Value : email; cmd.Parameters.Add("@id", SqlDbType.Int).Value = userId; if (cmd.ExecuteNonQuery()!=1) throw new InvalidOperationException("کاربر پیدا نشد."); }
            AuditService.TryWrite("UserEmailUpdated", "AppUsers", "UserId=" + userId);
        }
        public static DataTable GetHistory(int take = 250)
        {
            RequireAdmin();
            using (var cn = Db.Open()) using (var cmd = new SqlCommand(@"SELECT TOP (@take) e.Id, u.UserName, e.RecipientEmail, e.Subject, e.Status, e.AttemptCount, e.CreatedAtUtc, e.SentAtUtc, e.NextAttemptAtUtc, e.LastError FROM dbo.EmailNotificationOutbox e INNER JOIN dbo.AppUsers u ON u.UserId=e.UserId ORDER BY e.Id DESC", cn))
            { cmd.Parameters.Add("@take", SqlDbType.Int).Value = Math.Max(1, Math.Min(take, 1000)); using (var da = new SqlDataAdapter(cmd)) { var t=new DataTable(); da.Fill(t); return t; } }
        }
        public static void RetryDeadLetter(long queueId)
        {
            RequireAdmin(); if(queueId<=0) throw new ArgumentOutOfRangeException(nameof(queueId));
            using(var cn=Db.Open()) using(var cmd=new SqlCommand(@"UPDATE dbo.EmailNotificationOutbox SET Status=N'Pending',AttemptCount=0,NextAttemptAtUtc=SYSUTCDATETIME(),LastError=NULL WHERE Id=@id AND Status=N'DeadLetter'",cn))
            { cmd.Parameters.Add("@id",SqlDbType.BigInt).Value=queueId; if(cmd.ExecuteNonQuery()!=1) throw new InvalidOperationException("پیام ناموفق پیدا نشد یا وضعیت آن DeadLetter نیست."); }
            AuditService.TryWrite("EmailRetryRequested","EmailNotificationOutbox","QueueId="+queueId);
        }
    }
}
