using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    public static class UserManagementService
    {
        private const int Iterations = 150000;

        public static void EnsureSchema()
        {
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.AppUsers(
  UserId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUsers PRIMARY KEY,
  UserName nvarchar(80) NOT NULL,
  DisplayName nvarchar(120) NOT NULL,
  RoleName nvarchar(30) NOT NULL,
  PasswordSalt varbinary(32) NOT NULL,
  PasswordHash varbinary(32) NOT NULL,
  IsActive bit NOT NULL CONSTRAINT DF_AppUsers_IsActive DEFAULT(1),
  FailedLoginCount int NOT NULL CONSTRAINT DF_AppUsers_FailedLoginCount DEFAULT(0),
  LockedUntil datetime2 NULL,
  CreatedAt datetime2 NOT NULL CONSTRAINT DF_AppUsers_CreatedAt DEFAULT(SYSUTCDATETIME()),
  CONSTRAINT UQ_AppUsers_UserName UNIQUE(UserName),
  CONSTRAINT CK_AppUsers_Role CHECK(RoleName IN ('Administrator','Accountant','Manager'))
 );
END
IF COL_LENGTH(N'dbo.AppUsers',N'Email') IS NULL ALTER TABLE dbo.AppUsers ADD Email nvarchar(254) NULL", cn))
                cmd.ExecuteNonQuery();
        }

        private static void RequireAdmin()
        {
            AuthorizationService.RequireActiveSession("users");
        }

        public static DataTable GetUsers()
        {
            RequireAdmin();
            EnsureSchema();
            using (var cn = Db.Open())
            using (var da = new SqlDataAdapter("SELECT UserId, UserName, DisplayName, RoleName, Email, IsActive, FailedLoginCount, LockedUntil, CreatedAt FROM dbo.AppUsers ORDER BY UserName", cn))
            {
                var table = new DataTable(); da.Fill(table); return table;
            }
        }

        public static void Create(string userName, string displayName, string role, string password)
        {
            RequireAdmin();
            Validate(userName, displayName, role, password);
            EnsureSchema();
            var salt = new byte[32]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            byte[] hash; using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256)) hash = derive.GetBytes(32);
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"INSERT dbo.AppUsers(UserName,DisplayName,RoleName,PasswordSalt,PasswordHash) VALUES(@u,@d,@r,@s,@h)", cn))
            {
                cmd.Parameters.Add("@u", SqlDbType.NVarChar, 80).Value = userName.Trim();
                cmd.Parameters.Add("@d", SqlDbType.NVarChar, 120).Value = displayName.Trim();
                cmd.Parameters.Add("@r", SqlDbType.NVarChar, 30).Value = role;
                cmd.Parameters.Add("@s", SqlDbType.VarBinary, 32).Value = salt;
                cmd.Parameters.Add("@h", SqlDbType.VarBinary, 32).Value = hash;
                try { cmd.ExecuteNonQuery(); }
                catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627) { throw new InvalidOperationException("این نام کاربری قبلاً ثبت شده است.", ex); }
            }
            AuditService.TryWrite("UserCreated", "AppUsers", userName.Trim() + " / " + role);
        }

        public static void SetActive(int userId, bool active)
        {
            RequireAdmin();
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
            EnsureSchema();
            using (var cn = Db.Open())
            using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    using (var cmd = new SqlCommand(@"UPDATE dbo.AppUsers SET IsActive=@active
WHERE UserId=@id AND (
 @active=1 OR RoleName<>'Administrator' OR
 (SELECT COUNT(*) FROM dbo.AppUsers WITH (UPDLOCK,HOLDLOCK)
  WHERE RoleName='Administrator' AND IsActive=1)>1)", cn, tx))
                    {
                        cmd.Parameters.Add("@active", SqlDbType.Bit).Value = active;
                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = userId;
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("کاربر پیدا نشد یا غیرفعال‌کردن آخرین مدیر فعال مجاز نیست.");
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
            AuditService.TryWrite(active ? "UserActivated" : "UserDeactivated", "AppUsers", "UserId=" + userId);
        }

        private static void Validate(string userName, string displayName, string role, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || userName.Trim().Length < 3) throw new ArgumentException("نام کاربری باید حداقل ۳ نویسه باشد.");
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("نام نمایشی الزامی است.");
            if (role != AuthorizationService.Administrator && role != AuthorizationService.Accountant && role != AuthorizationService.Manager) throw new ArgumentException("نقش انتخاب‌شده معتبر نیست.");
            if (string.IsNullOrEmpty(password) || password.Length < 12 || password.IndexOfAny("0123456789".ToCharArray()) < 0 || password.IndexOfAny("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()) < 0 || password.IndexOfAny("abcdefghijklmnopqrstuvwxyz".ToCharArray()) < 0)
                throw new ArgumentException("رمز عبور باید حداقل ۱۲ نویسه و شامل حروف بزرگ، کوچک و عدد باشد.");
        }
    }
}
