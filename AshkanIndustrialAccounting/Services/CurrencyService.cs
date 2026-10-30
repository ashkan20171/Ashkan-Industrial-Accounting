using System.Globalization;

namespace AshkanIndustrialAccounting.Services
{
    public static class CurrencyService
    {
        // Portfolio/demo conversion rate. In production this should come from a controlled FX-rate table/API.
        public const decimal DemoUsdToIrrRate = 600000m;

        public static string CurrencyCode => LocalizationService.IsPersian ? "IRR" : "USD";
        public static string CurrencyName => LocalizationService.IsPersian ? "ریال" : "USD";

        public static decimal ToDisplayAmount(decimal baseUsdAmount)
        {
            return LocalizationService.IsPersian ? baseUsdAmount * DemoUsdToIrrRate : baseUsdAmount;
        }

        public static string FormatFromUsd(decimal baseUsdAmount)
        {
            var amount = ToDisplayAmount(baseUsdAmount);
            if (LocalizationService.IsPersian)
                return string.Format(CultureInfo.GetCultureInfo("fa-IR"), "{0:N0} ریال", amount);
            return string.Format(CultureInfo.GetCultureInfo("en-US"), "{0:C0}", amount);
        }

        public static string Format(decimal amount)
        {
            if (LocalizationService.IsPersian)
                return string.Format(CultureInfo.GetCultureInfo("fa-IR"), "{0:N0} ریال", amount);
            return string.Format(CultureInfo.GetCultureInfo("en-US"), "{0:C2}", amount);
        }
    }
}
