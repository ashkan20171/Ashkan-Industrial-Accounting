using System;
using System.Windows.Forms;
using AshkanIndustrialAccounting.App_Start;
using AshkanIndustrialAccounting.UI.Forms;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppBootstrapper.Initialize();
            // A closed main window returns to sign-in only for explicit logout or expiry.
            bool reopenLogin = true;
            while (reopenLogin)
            {
                AuthorizationService.EndSession();
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) break;
                }
                using (var main = new MainForm())
                {
                    Application.Run(main);
                    reopenLogin = main.ReturnToLogin;
                }
                AuthorizationService.EndSession();
            }
        }
    }
}
