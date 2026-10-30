using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AshkanIndustrialAccounting.Services
{
    // Local administrator bootstrap. For multi-user deployment replace with SQL-backed identities.
    public static class SecurityService
    {
        private const int Iterations = 150000;
        private static readonly string CredentialPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AshkanIndustrialAccounting", "admin.credentials");
        public static bool RequiresSetup { get { return !File.Exists(CredentialPath); } }

        public static void CreateAdministrator(string username, string password)
        {
            if (!RequiresSetup) throw new InvalidOperationException("Administrator already configured.");
            Validate(username, password);
            var salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            byte[] hash;
            using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                hash = derive.GetBytes(32);
            var folder = Path.GetDirectoryName(CredentialPath);
            Directory.CreateDirectory(folder);
            var temp = CredentialPath + ".tmp";
            File.WriteAllLines(temp, new[] { "v1", Convert.ToBase64String(Encoding.UTF8.GetBytes(username.Trim())),
                Convert.ToBase64String(salt), Convert.ToBase64String(hash) });
            try { File.Move(temp, CredentialPath); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        public static bool Authenticate(string username, string password)
        {
            if (RequiresSetup || username == null || password == null) return false;
            try
            {
                var parts = File.ReadAllLines(CredentialPath);
                if (parts.Length != 4 || parts[0] != "v1") return false;
                var storedUser = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                if (!string.Equals(storedUser, username.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                byte[] actual;
                using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                    actual = derive.GetBytes(expected.Length);
                var difference = actual.Length ^ expected.Length;
                for (var i = 0; i < Math.Min(actual.Length, expected.Length); i++) difference |= actual[i] ^ expected[i];
                return difference == 0;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (FormatException) { return false; }
            catch (CryptographicException) { return false; }
        }

        private static void Validate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
                throw new ArgumentException("Username must contain at least 3 characters.");
            if (string.IsNullOrEmpty(password) || password.Length < 12 ||
                password.IndexOfAny("0123456789".ToCharArray()) < 0 ||
                password.IndexOfAny("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()) < 0 ||
                password.IndexOfAny("abcdefghijklmnopqrstuvwxyz".ToCharArray()) < 0)
                throw new ArgumentException("Password needs 12+ characters, uppercase, lowercase and a digit.");
        }
    }
}
