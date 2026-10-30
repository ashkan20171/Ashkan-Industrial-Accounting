using System.Collections.Generic;
namespace AshkanIndustrialAccounting.Services
{
    public static class LocalizationService
    {
        public static string Language { get; private set; } = "fa";
        public static bool IsPersian => Language == "fa";
        public static void SetLanguage(string language) { Language = language == "fa" ? "fa" : "en"; }
        private static readonly Dictionary<string,string> En = new Dictionary<string,string> {
            ["dashboard"]="Dashboard",["accounts"]="Chart of Accounts",["journal"]="Journal Entry",["sales"]="Sales & Invoices",["products"]="Products & Inventory",["cashbank"]="Cash & Bank",["partners"]="Customers & Suppliers",["costing"]="Industrial Costing",
            ["reports"]="Financial Reports",["settings"]="Settings",["database"]="Database Connection",["users"]="User Management",["logout"]="Logout",["language"]="فارسی",
            ["revenue"]="Revenue",["expenses"]="Expenses",["cash"]="Cash & Bank",["receivables"]="Receivables",
            ["notifications"]="Notifications",["services"]="Service Monitoring",["title"]="Ashkan Industrial Accounting"
        };
        private static readonly Dictionary<string,string> Fa = new Dictionary<string,string> {
            ["dashboard"]="داشبورد",["accounts"]="کدینگ حساب‌ها",["journal"]="سند حسابداری",["sales"]="فروش و فاکتورها",["products"]="کالا و انبار",["cashbank"]="صندوق و بانک",["partners"]="مشتریان و تأمین‌کنندگان",["costing"]="بهای تمام‌شده صنعتی",
            ["reports"]="گزارش‌های مالی",["settings"]="تنظیمات",["database"]="اتصال دیتابیس",["users"]="مدیریت کاربران",["logout"]="خروج",["language"]="English",
            ["revenue"]="درآمد",["expenses"]="هزینه‌ها",["cash"]="نقد و بانک",["receivables"]="حساب‌های دریافتنی",
            ["notifications"]="مرکز اعلان‌ها",["services"]="پایش سرویس‌ها",["title"]="حسابداری صنعتی اشکان"
        };
        public static string T(string key)
        {
            var d=IsPersian?Fa:En; return d.ContainsKey(key)?d[key]:key;
        }
    }
}
