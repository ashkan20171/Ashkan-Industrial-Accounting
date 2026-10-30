using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    public static class SqlAuthenticationService
    {
        private const int Iterations = 150000;
        public static void ChangePassword(string username, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(oldPassword))
                throw new ArgumentException("Current credentials are required.");
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 12 ||
                newPassword.IndexOfAny("0123456789".ToCharArray()) < 0 ||
                newPassword.IndexOfAny("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()) < 0 ||
                newPassword.IndexOfAny("abcdefghijklmnopqrstuvwxyz".ToCharArray()) < 0)
                throw new ArgumentException("New password needs 12+ characters, uppercase, lowercase and a digit.");
            string role;
            if (!TryLogin(username, oldPassword, out role))
                throw new UnauthorizedAccessException("Current password is incorrect or the account is locked.");
            byte[] salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            byte[] hash;
            using (var derive = new Rfc2898DeriveBytes(newPassword, salt, Iterations, HashAlgorithmName.SHA256))
                hash = derive.GetBytes(32);
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"UPDATE dbo.AppUsers SET PasswordSalt=@salt,PasswordHash=@hash,FailedLoginCount=0,LockedUntil=NULL WHERE UserName=@user AND IsActive=1", cn))
            {
                cmd.Parameters.Add("@salt", SqlDbType.VarBinary, 32).Value = salt;
                cmd.Parameters.Add("@hash", SqlDbType.VarBinary, 32).Value = hash;
                cmd.Parameters.Add("@user", SqlDbType.NVarChar, 80).Value = username.Trim();
                if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Account update failed.");
            }
        }

        public static bool TryLogin(string username, string password, out string role)
        {
            role = null;
            if (string.IsNullOrWhiteSpace(username) || password == null) return false;
            UserManagementService.EnsureSchema();
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT UserId,RoleName,PasswordSalt,PasswordHash,IsActive,LockedUntil FROM dbo.AppUsers WHERE UserName=@u", cn))
            {
                cmd.Parameters.Add("@u", SqlDbType.NVarChar, 80).Value = username.Trim();
                int id; string foundRole; byte[] salt, expected; bool active; DateTime? locked;
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return false;
                    id = reader.GetInt32(0); foundRole = reader.GetString(1);
                    salt = (byte[])reader[2]; expected = (byte[])reader[3]; active = reader.GetBoolean(4);
                    locked = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
                }
                if (!active || (locked.HasValue && locked.Value > DateTime.UtcNow)) return false;
                byte[] actual;
                using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                    actual = derive.GetBytes(expected.Length);
                int diff = actual.Length ^ expected.Length;
                for (int i = 0; i < Math.Min(actual.Length, expected.Length); i++) diff |= actual[i] ^ expected[i];
                bool valid = diff == 0;
                using (var update = new SqlCommand(valid
                    ? "UPDATE dbo.AppUsers SET FailedLoginCount=0,LockedUntil=NULL WHERE UserId=@id"
                    : "UPDATE dbo.AppUsers SET FailedLoginCount=FailedLoginCount+1, LockedUntil=CASE WHEN FailedLoginCount+1>=5 THEN DATEADD(minute,15,SYSUTCDATETIME()) ELSE LockedUntil END WHERE UserId=@id", cn))
                {
                    update.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    update.ExecuteNonQuery();
                }
                if (valid) role = foundRole;
                return valid;
            }
        }
    }
}
