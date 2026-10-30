using System;
using System.Collections.Generic;

namespace AshkanIndustrialAccounting.Services
{
    public static class AuthorizationService
    {
        public const string Administrator = "Administrator";
        public const string Accountant = "Accountant";
        public const string Manager = "Manager";
        public static string CurrentUserName { get; private set; }
        public static string CurrentRole { get; private set; }

        public static bool IsSqlBacked { get; private set; }
        public static DateTime SessionStartedUtc { get; private set; }
        public static DateTime LastActivityUtc { get; private set; }
        public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);

        public static void EndSession()
        {
            CurrentUserName = null;
            CurrentRole = null;
            IsSqlBacked = false;
            SessionStartedUtc = DateTime.MinValue;
            LastActivityUtc = DateTime.MinValue;
        }

        public static void StartSession(string userName, string role, bool sqlBacked = false)
        {
            if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("A username is required.", nameof(userName));
            if (role != Administrator && role != Accountant && role != Manager)
                throw new ArgumentException("Unknown role.", nameof(role));
            CurrentUserName = userName.Trim(); CurrentRole = role;
            IsSqlBacked = sqlBacked;
            SessionStartedUtc = LastActivityUtc = DateTime.UtcNow;
        }

        public static void RequireActiveSession(string feature)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserName) ||
                DateTime.UtcNow - LastActivityUtc > IdleTimeout)
            {
                EndSession();
                throw new UnauthorizedAccessException("Session expired. Please sign in again.");
            }
            if (IsSqlBacked)
            {
                using (var cn = Infrastructure.Data.Db.Open())
                using (var cmd = new System.Data.SqlClient.SqlCommand(
                    "SELECT RoleName FROM dbo.AppUsers WHERE UserName=@u AND IsActive=1 AND (LockedUntil IS NULL OR LockedUntil<=SYSUTCDATETIME())", cn))
                {
                    cmd.Parameters.Add("@u", System.Data.SqlDbType.NVarChar, 80).Value = CurrentUserName;
                    var role = cmd.ExecuteScalar() as string;
                    if (role == null || role != CurrentRole)
                    {
                        EndSession();
                        throw new UnauthorizedAccessException("Account disabled, locked, or role changed. Sign in again.");
                    }
                }
            }
            if (!CanAccess(feature)) throw new UnauthorizedAccessException("Permission denied: " + feature);
            LastActivityUtc = DateTime.UtcNow;
        }

        public static bool CanAccess(string feature)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserName) ||
                DateTime.UtcNow - LastActivityUtc > IdleTimeout) return false;
            if (feature == "auditlog") return CurrentRole == Administrator;
            if (CurrentRole == Administrator) return true;
            switch (feature)
            {
                case "dashboard": case "reports": case "notifications": return true;
                case "services": return CurrentRole == Administrator;
                case "accounts": case "journal": case "partners": case "cashbank":
                    return CurrentRole == Accountant || CurrentRole == Manager;
                case "sales": case "products": case "costing":
                    return CurrentRole == Manager || CurrentRole == Accountant;
                case "settings": case "database": case "users": return false;
                case "language": return true;
                default: return false;
            }
        }

        public static IEnumerable<string> GetDeniedFeatures()
        {
            foreach (var feature in new[] { "dashboard", "sales", "products", "cashbank", "accounts", "journal", "partners", "costing", "reports", "settings", "database", "users", "language", "notifications", "services" })
                if (!CanAccess(feature)) yield return feature;
        }
    }
}
