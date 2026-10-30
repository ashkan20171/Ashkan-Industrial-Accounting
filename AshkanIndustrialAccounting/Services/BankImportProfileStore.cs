using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Per-Windows-user, local column mappings. No credentials or financial rows are persisted.</summary>
    public sealed class BankImportProfile
    {
        public string Name { get; set; }
        public int[] Columns { get; set; }
        public bool HasHeader { get; set; }
    }
    public static class BankImportProfileStore
    {
        private static readonly string DirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AshkanIndustrialAccounting", "BankImportProfiles");
        private static string SafePath(string name)
        {
            if (String.IsNullOrWhiteSpace(name) || name.Trim().Length > 60)
                throw new ArgumentException("Profile name must contain 1-60 characters.");
            // A hash-free stable, reversible encoding avoids path traversal and invalid filename characters.
            var bytes = Encoding.UTF8.GetBytes(name.Trim());
            return Path.Combine(DirectoryPath, BitConverter.ToString(bytes).Replace("-", "") + ".map");
        }
        public static List<BankImportProfile> Load()
        {
            var profiles = new List<BankImportProfile>();
            if (!Directory.Exists(DirectoryPath)) return profiles;
            foreach (var file in Directory.GetFiles(DirectoryPath, "*.map"))
            {
                try
                {
                    var lines = File.ReadAllLines(file, Encoding.UTF8);
                    if (lines.Length != 3) continue;
                    var name = Encoding.UTF8.GetString(Convert.FromBase64String(lines[0]));
                    var values = lines[1].Split(',');
                    if (values.Length != 4) continue;
                    var columns = new int[4];
                    for (int i=0;i<4;i++) columns[i] = Int32.Parse(values[i], System.Globalization.CultureInfo.InvariantCulture);
                    BankStatementCsv.ValidateColumns(columns);
                    if (lines[2] != "1" && lines[2] != "0") continue;
                    if (!String.Equals(file, SafePath(name), StringComparison.OrdinalIgnoreCase)) continue;
                    profiles.Add(new BankImportProfile { Name=name, Columns=columns, HasHeader=lines[2]=="1" });
                }
                catch (Exception) { /* Ignore corrupted profiles; never execute profile content. */ }
            }
            profiles.Sort((a,b)=>StringComparer.CurrentCultureIgnoreCase.Compare(a.Name,b.Name));
            return profiles;
        }
        public static void Save(BankImportProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            BankStatementCsv.ValidateColumns(profile.Columns);
            var target=SafePath(profile.Name);
            Directory.CreateDirectory(DirectoryPath);
            var content=Convert.ToBase64String(Encoding.UTF8.GetBytes(profile.Name.Trim()))+"\n"+
                String.Join(",",profile.Columns)+"\n"+(profile.HasHeader?"1":"0")+"\n";
            var temp=target+".tmp";
            File.WriteAllText(temp,content,new UTF8Encoding(false));
            try { if (File.Exists(target)) File.Delete(target); File.Move(temp,target); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        public static void Delete(string name)
        {
            var path=SafePath(name);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
