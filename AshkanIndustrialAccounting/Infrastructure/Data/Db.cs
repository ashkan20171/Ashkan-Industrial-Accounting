using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace AshkanIndustrialAccounting.Infrastructure.Data
{
    public static class Db
    {
        private static string _workingConnectionString;
        public static string LastError { get; private set; }
        public static string ActiveServer { get; private set; }

        public static SqlConnection Open()
        {
            if (!string.IsNullOrWhiteSpace(_workingConnectionString))
            {
                var cached = new SqlConnection(_workingConnectionString);
                try { cached.Open(); return cached; }
                catch { cached.Dispose(); _workingConnectionString = null; }
            }

            Exception last = null;
            foreach (var cs in CandidateConnectionStrings())
            {
                try
                {
                    var cn = new SqlConnection(cs);
                    cn.Open();
                    _workingConnectionString = cs;
                    ActiveServer = new SqlConnectionStringBuilder(cs).DataSource;
                    LastError = null;
                    return cn;
                }
                catch (Exception ex) { last = ex; }
            }
            LastError = last == null ? "No SQL Server connection candidate is configured." : last.Message;
            throw new InvalidOperationException(
                "No usable SQL Server instance was found. Install SQL Server Express LocalDB / SQL Server Express, or edit App.config. Last error: " + LastError, last);
        }

        public static IEnumerable<string> CandidateConnectionStrings()
        {
            var configured = ConfigurationManager.ConnectionStrings["AccountingDb"];
            if (configured != null && !string.IsNullOrWhiteSpace(configured.ConnectionString))
                yield return configured.ConnectionString;

            yield return @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=AshkanIndustrialAccounting;Integrated Security=True;Connect Timeout=4";
            yield return @"Data Source=.\SQLEXPRESS;Initial Catalog=AshkanIndustrialAccounting;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=4";
            yield return @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AshkanIndustrialAccounting;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=4";
            yield return @"Data Source=localhost;Initial Catalog=AshkanIndustrialAccounting;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=4";
        }

        public static void UseConnectionString(string connectionString)
        {
            _workingConnectionString = connectionString;
            ActiveServer = new SqlConnectionStringBuilder(connectionString).DataSource;
        }
    }
}