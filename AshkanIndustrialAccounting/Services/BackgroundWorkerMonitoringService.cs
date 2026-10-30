using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    public static class BackgroundWorkerMonitoringService
    {
        public static DataTable GetOverview()
        {
            AuthorizationService.RequireActiveSession("services");
            const string sql = @"SELECT names.WorkerName, s.LastHeartbeatUtc,
CASE WHEN s.WorkerName IS NULL THEN N'Not reported' WHEN DATEDIFF(SECOND,s.LastHeartbeatUtc,SYSUTCDATETIME())>90 THEN N'Offline' WHEN s.LastStatus=N'Error' THEN N'Error' ELSE N'Online' END AS RuntimeStatus,
s.LastStatus, s.LastError,
DATEDIFF(SECOND,s.LastHeartbeatUtc,SYSUTCDATETIME()) AS SecondsSinceHeartbeat
FROM (SELECT N'BankNotificationWorker' AS WorkerName UNION ALL SELECT N'EmailNotificationWorker') names
LEFT JOIN dbo.BackgroundWorkerStatus s ON s.WorkerName=names.WorkerName ORDER BY names.WorkerName;";
            using (var cn = Db.Open()) using (var cmd = new SqlCommand(sql, cn)) using (var da = new SqlDataAdapter(cmd))
            { var table = new DataTable(); da.Fill(table); return table; }
        }
        public static DataTable GetQueueSummary()
        {
            AuthorizationService.RequireActiveSession("services");
            const string sql = @"SELECT N'Bank alerts' AS QueueName,
COALESCE(SUM(CASE WHEN ProcessedAtUtc IS NULL THEN 1 ELSE 0 END),0) AS Pending,
COALESCE(SUM(CASE WHEN ProcessedAtUtc IS NOT NULL THEN 1 ELSE 0 END),0) AS Completed,
COALESCE(SUM(CASE WHEN ProcessedAtUtc IS NULL AND AttemptCount>0 THEN 1 ELSE 0 END),0) AS Retried FROM dbo.BankAlertNotificationOutbox
UNION ALL
SELECT N'Email',COALESCE(SUM(CASE WHEN Status=N'Pending' THEN 1 ELSE 0 END),0),COALESCE(SUM(CASE WHEN Status=N'Sent' THEN 1 ELSE 0 END),0),COALESCE(SUM(CASE WHEN Status=N'DeadLetter' THEN 1 ELSE 0 END),0) FROM dbo.EmailNotificationOutbox;";
            using (var cn=Db.Open()) using(var cmd=new SqlCommand(sql,cn)) using(var da=new SqlDataAdapter(cmd))
            { var table=new DataTable(); da.Fill(table); return table; }
        }
        public static DataTable GetRecentHistory(int hours)
        {
            AuthorizationService.RequireActiveSession("services");
            if (hours < 1 || hours > 168) throw new ArgumentOutOfRangeException("hours");
            const string sql = @"SELECT TOP(500) WorkerName,HeartbeatUtc,Status,ErrorMessage
FROM dbo.BackgroundWorkerHistory
WHERE HeartbeatUtc>=DATEADD(HOUR,-@hours,SYSUTCDATETIME())
ORDER BY HeartbeatUtc DESC;";
            using (var cn=Db.Open()) using(var cmd=new SqlCommand(sql,cn))
            {
                cmd.Parameters.Add("@hours",SqlDbType.Int).Value=hours;
                using(var da=new SqlDataAdapter(cmd)) { var table=new DataTable(); da.Fill(table); return table; }
            }
        }
        public static DataTable GetDailyMetrics(int days)
        {
            AuthorizationService.RequireActiveSession("services");
            if (days < 1 || days > 31) throw new ArgumentOutOfRangeException("days");
            const string sql = @"SELECT CONVERT(date,HeartbeatUtc) AS [MetricDate],WorkerName,
SUM(CASE WHEN Status=N'Healthy' THEN 1 ELSE 0 END) AS HealthyCount,
SUM(CASE WHEN Status=N'Error' THEN 1 ELSE 0 END) AS ErrorCount,
COUNT_BIG(*) AS HeartbeatCount
FROM dbo.BackgroundWorkerHistory
WHERE HeartbeatUtc>=DATEADD(DAY,-@days,SYSUTCDATETIME())
GROUP BY CONVERT(date,HeartbeatUtc),WorkerName ORDER BY MetricDate DESC,WorkerName;";
            using (var cn=Db.Open()) using(var cmd=new SqlCommand(sql,cn))
            {
                cmd.Parameters.Add("@days",SqlDbType.Int).Value=days;
                using(var da=new SqlDataAdapter(cmd)) { var table=new DataTable(); da.Fill(table); return table; }
            }
        }

    }
}
