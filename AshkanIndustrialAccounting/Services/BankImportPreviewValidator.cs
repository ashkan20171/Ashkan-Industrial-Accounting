using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Validates editable statement preview without writing to the database.</summary>
    public static class BankImportPreviewValidator
    {
        public static List<BankStatementCsvRow> Validate(DataTable preview, ISet<string> existing)
        {
            return Validate(preview, existing, false);
        }
        public static List<BankStatementCsvRow> Validate(DataTable preview, ISet<string> existing, bool persian)
        {
            if (preview == null) throw new ArgumentNullException("preview");
            if (existing == null) throw new ArgumentNullException("existing");
            var ready = new List<BankStatementCsvRow>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in preview.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;
                string dateText = Convert.ToString(row["Date"]) ?? "";
                string reference = (Convert.ToString(row["Reference"]) ?? "").Trim();
                string amountText = Convert.ToString(row["Amount"]) ?? "";
                string description = Convert.ToString(row["Description"]) ?? "";
                DateTime date;
                decimal amount;
                string error = null;
                if (!DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out date)) error = "Invalid date (yyyy-MM-dd)";
                else if (reference.Length == 0 || reference.Length > 100) error = "Reference required (max 100)";
                else if (!Decimal.TryParse(amountText, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out amount) || amount == 0m ||
                    amount != Decimal.Round(amount, 2) || Math.Abs(amount) > 9999999999999999m)
                    error = "Invalid amount (nonzero, max 2 decimals)";
                else if (description.Length > 500) error = "Description exceeds 500 characters";
                else if (!seen.Add(reference)) error = "Duplicate reference in file";
                else if (existing.Contains(reference)) error = "Reference already exists in database";
                row["Status"] = error == null ? "Ready" : (persian ? TranslateError(error) : error);
                if (error == null)
                    ready.Add(new BankStatementCsvRow { Date = date, Reference = reference,
                        Amount = amount, Description = description });
            }
            return ready;
        }
        private static string TranslateError(string error)
        {
            switch (error)
            {
                case "Invalid date (yyyy-MM-dd)": return "تاریخ نامعتبر (yyyy-MM-dd)";
                case "Reference required (max 100)": return "شناسه مرجع الزامی است (حداکثر ۱۰۰ نویسه)";
                case "Invalid amount (nonzero, max 2 decimals)": return "مبلغ نامعتبر (غیرصفر، حداکثر دو رقم اعشار)";
                case "Description exceeds 500 characters": return "شرح بیشتر از ۵۰۰ نویسه است";
                case "Duplicate reference in file": return "شناسه مرجع تکراری در فایل";
                case "Reference already exists in database": return "شناسه مرجع قبلاً در پایگاه داده ثبت شده است";
                default: return error;
            }
        }
    }
}
