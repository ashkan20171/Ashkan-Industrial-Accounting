using System.Globalization;
using System.Threading;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.App_Start
{
    public static class AppBootstrapper
    {
        public static string DatabaseStartupError { get; private set; }

        public static void Initialize()
        {
            LocalizationService.SetLanguage("fa");
            var culture = CultureInfo.GetCultureInfo("fa-IR");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            try { DbInitializer.EnsureCreated(); }
            catch (System.Exception ex) { DatabaseStartupError = ex.Message; }
        }
    }
}