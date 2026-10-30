using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using AshkanIndustrialAccounting.App_Start;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class MainForm : Form
    {
        private readonly Panel _nav = new Panel();
        private readonly Panel _navItems = new Panel();
        private readonly Panel _host = new Panel();
        private readonly Panel _top = new Panel();
        private readonly Label _brand = new Label();
        private readonly Label _pageTitle = new Label();
        private readonly Label _user = new Label();
        private readonly Button[] _buttons = new Button[15];
        private readonly Button _passwordButton = new Button();
        private readonly Button _logoutButton = new Button();
        private readonly System.Windows.Forms.Timer _sessionTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _notificationTimer = new System.Windows.Forms.Timer();
        public bool ReturnToLogin { get; private set; }
        private string _activePage = "dashboard";

        public MainForm()
        {
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1180, 720);
            BackColor = Color.FromArgb(242, 246, 251);
            Font = new Font("Segoe UI", 10F);

            _nav.Width = 270;
            _nav.Dock = DockStyle.Left;
            _navItems.Dock = DockStyle.Fill;
            _navItems.AutoScroll = true;
            _navItems.BackColor = Color.FromArgb(15, 31, 52);
            _nav.BackColor = Color.FromArgb(15, 31, 52);

            _top.Dock = DockStyle.Top;
            _top.Height = 76;
            _top.BackColor = Color.White;
            _top.Padding = new Padding(26, 0, 26, 0);

            _pageTitle.Dock = DockStyle.Fill;
            _pageTitle.Font = new Font("Segoe UI Semibold", 18F);
            _pageTitle.ForeColor = Color.FromArgb(22, 43, 70);
            _pageTitle.TextAlign = ContentAlignment.MiddleLeft;

            _user.Dock = DockStyle.Right;
            _user.Width = 240;
            _user.Text = "●  " + AuthorizationService.CurrentUserName + " · " + AuthorizationService.CurrentRole;
            _user.TextAlign = ContentAlignment.MiddleRight;
            _user.ForeColor = Color.FromArgb(77, 94, 115);

            _passwordButton.Dock = DockStyle.Right;
            _passwordButton.Width = 155;
            _passwordButton.FlatStyle = FlatStyle.Flat;
            _passwordButton.FlatAppearance.BorderSize = 0;
            _passwordButton.ForeColor = Color.FromArgb(25, 99, 180);
            _passwordButton.BackColor = Color.FromArgb(237, 245, 255);
            _passwordButton.Cursor = Cursors.Hand;
            _passwordButton.Click += (s, e) => {
                if (!VerifySession("dashboard")) return;
                using (var dialog = new ChangePasswordForm()) dialog.ShowDialog(this);
            };
            _logoutButton.Dock = DockStyle.Right;
            _logoutButton.Width = 110;
            _logoutButton.FlatStyle = FlatStyle.Flat;
            _logoutButton.FlatAppearance.BorderSize = 0;
            _logoutButton.BackColor = Color.FromArgb(255, 236, 233);
            _logoutButton.ForeColor = Color.FromArgb(150, 52, 44);
            _logoutButton.Click += (s, e) => Logout(false);
            _top.Controls.Add(_pageTitle);
            _top.Controls.Add(_logoutButton);
            _top.Controls.Add(_passwordButton);
            _top.Controls.Add(_user);

            _host.Dock = DockStyle.Fill;
            _host.Padding = new Padding(26);
            _host.BackColor = Color.FromArgb(242, 246, 251);

            _brand.Dock = DockStyle.Top;
            _brand.Height = 104;
            _brand.ForeColor = Color.White;
            _brand.Font = new Font("Segoe UI Semibold", 15F);
            _brand.TextAlign = ContentAlignment.MiddleCenter;
            _nav.Controls.Add(_navItems);
            _nav.Controls.Add(_brand);

            string[] keys = { "dashboard", "sales", "products", "cashbank", "accounts", "journal", "partners", "costing", "reports", "settings", "database", "users", "language", "notifications", "services" };
            for (int i = keys.Length - 1; i >= 0; i--)
            {
                var b = MakeButton();
                b.Tag = keys[i];
                _buttons[i] = b;
                _navItems.Controls.Add(b);
            }
            _brand.BringToFront();
            _navItems.Resize += (s, e) => { foreach (var button in _buttons) if (button != null) button.Width = _navItems.ClientSize.Width; };

            Controls.Add(_host);
            Controls.Add(_top);
            Controls.Add(_nav);

            _sessionTimer.Interval = 60000;
            _sessionTimer.Tick += (s, e) =>
            {
                try { AuthorizationService.RequireActiveSession("dashboard"); }
                catch (UnauthorizedAccessException) { Logout(true); }
                catch (System.Data.SqlClient.SqlException) { Logout(true); }
            };
            _sessionTimer.Start();
            _notificationTimer.Interval = 30000;
            _notificationTimer.Tick += (s, e) => RefreshNotificationBadge();
            _notificationTimer.Start();
            FormClosed += (s, e) => { _sessionTimer.Stop(); _sessionTimer.Dispose(); _notificationTimer.Stop(); _notificationTimer.Dispose(); };
            ApplyLanguage();
            RefreshNotificationBadge();
            Navigate("dashboard");

            if (!string.IsNullOrWhiteSpace(AppBootstrapper.DatabaseStartupError))
                MessageBox.Show(
                    (LocalizationService.IsPersian ? "اتصال اولیه به دیتابیس انجام نشد:\n\n" : "Database initialization failed:\n\n")
                    + AppBootstrapper.DatabaseStartupError +
                    (LocalizationService.IsPersian ? "\n\nدر صورت نصب نبودن LocalDB، از Visual Studio Installer گزینه SQL Server Express LocalDB را نصب کنید." :
                    "\n\nIf LocalDB is not installed, install SQL Server Express LocalDB from Visual Studio Installer."),
                    LocalizationService.IsPersian ? "وضعیت دیتابیس" : "Database status",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void Logout(bool expired)
        {
            if (IsDisposed || Disposing) return;
            _sessionTimer.Stop();
            AuditService.TryWrite(expired ? "SessionExpired" : "Logout", "AppUsers");
            AuthorizationService.EndSession();
            ReturnToLogin = true;
            if (expired)
                MessageBox.Show(LocalizationService.IsPersian ?
                    "نشست شما پایان یافته یا دسترسی حساب تغییر کرده است. دوباره وارد شوید." :
                    "Your session expired or your account permissions changed. Please sign in again.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private bool VerifySession(string feature)
        {
            try { AuthorizationService.RequireActiveSession(feature); return true; }
            catch (UnauthorizedAccessException ex)
            {
                if (string.IsNullOrWhiteSpace(AuthorizationService.CurrentUserName)) Logout(true);
                else MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            catch (System.Data.SqlClient.SqlException)
            {
                MessageBox.Show(LocalizationService.IsPersian ?
                    "امکان بررسی اعتبار نشست وجود ندارد." : "Session validation is unavailable.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        private Button MakeButton()
        {
            var b = new Button {
                Dock = DockStyle.Top, Height = 58, FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(215, 226, 239), BackColor = Color.FromArgb(15, 31, 52),
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(26, 0, 0, 0),
                Font = new Font("Segoe UI", 10.5F), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 99, 180);
            b.Click += NavClick;
            return b;
        }

        private void NavClick(object sender, EventArgs e)
        {
            var key = (string)((Button)sender).Tag;
            if (!VerifySession(key)) return;
            if (key == "language")
            {
                LocalizationService.SetLanguage(LocalizationService.IsPersian ? "en" : "fa");
                var culture = CultureInfo.GetCultureInfo(LocalizationService.IsPersian ? "fa-IR" : "en-US");
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                ApplyLanguage();
                Navigate("dashboard");
            }
            else Navigate(key);
        }

        private void ApplyLanguage()
        {
            Text = LocalizationService.T("title");
            _brand.Text = "◆  " + LocalizationService.T("title");
            RightToLeft = LocalizationService.IsPersian ? RightToLeft.Yes : RightToLeft.No;
            RightToLeftLayout = LocalizationService.IsPersian;
            _nav.Dock = LocalizationService.IsPersian ? DockStyle.Right : DockStyle.Left;
            _navItems.RightToLeft = LocalizationService.IsPersian ? RightToLeft.Yes : RightToLeft.No;
            _pageTitle.TextAlign = LocalizationService.IsPersian ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            _user.Dock = LocalizationService.IsPersian ? DockStyle.Left : DockStyle.Right;
            _user.TextAlign = LocalizationService.IsPersian ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight;
            _user.Text = "●  " + AuthorizationService.CurrentUserName + " · " + AuthorizationService.CurrentRole;
            _passwordButton.Text = LocalizationService.IsPersian ? "تغییر رمز عبور" : "Change password";
            _logoutButton.Text = LocalizationService.IsPersian ? "خروج از حساب" : "Sign out";
            foreach (var b in _buttons)
            {
                b.Text = "   " + LocalizationService.T((string)b.Tag);
                b.Enabled = AuthorizationService.CanAccess((string)b.Tag);
                b.ForeColor = b.Enabled ? Color.FromArgb(215, 226, 239) : Color.FromArgb(100, 112, 128);
                b.BackColor = b.Enabled && (string)b.Tag == _activePage ? Color.FromArgb(25, 99, 180) : Color.FromArgb(15, 31, 52);
                b.TextAlign = LocalizationService.IsPersian ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                b.Padding = LocalizationService.IsPersian ? new Padding(0, 0, 26, 0) : new Padding(26, 0, 0, 0);
            }
            RefreshNotificationBadge();
        }

        private void RefreshNotificationBadge()
        {
            var button = _buttons.FirstOrDefault(x => x != null && Convert.ToString(x.Tag) == "notifications");
            if (button == null) return;
            try
            {
                int count = NotificationInboxService.GetUnreadCount();
                button.Text = "   " + LocalizationService.T("notifications") + (count > 0 ? "  (" + count + ")" : "");
                button.ForeColor = count > 0 ? Color.FromArgb(255, 213, 110) : Color.FromArgb(215, 226, 239);
            }
            catch (Exception) { /* Keep navigation responsive while the database is temporarily unavailable. */ }
        }

        public void NavigateToFeature(string key)
        {
            if (IsDisposed || Disposing) return;
            if (!VerifySession(key)) return;
            Navigate(key);
        }

        private void Navigate(string key)
        {
            if (!VerifySession(key)) return;
            if (!AuthorizationService.CanAccess(key))
            {
                MessageBox.Show(LocalizationService.IsPersian ? "شما مجوز دسترسی به این بخش را ندارید." : "You do not have permission to access this section.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AuditService.TryWrite("AccessDenied", key);
                return;
            }
            AuditService.TryWrite("Navigate", key);
            _activePage = key;
            ApplyLanguage();
            foreach (Control old in _host.Controls.Cast<Control>().ToArray())
            {
                _host.Controls.Remove(old);
                old.Dispose();
            }
            _pageTitle.Text = LocalizationService.T(key);
            Form f;
            switch (key)
            {
                case "sales": f = new SalesForm(); break;
                case "products": f = new ProductsForm(); break;
                case "cashbank": f = new CashBankForm(); break;
                case "accounts": f = new ChartOfAccountsForm(); break;
                case "journal": f = new JournalEntryForm(); break;
                case "partners": f = new BusinessPartnersForm(); break;
                case "costing": f = new IndustrialCostingForm(); break;
                case "reports": f = new ReportsForm(); break;
                case "settings": f = new SettingsForm(); break;
                case "database": f = new DatabaseSetupForm(); break;
                case "users": f = new UserManagementForm(); break;
                case "notifications": f = new NotificationCenterForm(); break;
                case "services": f = new ServiceMonitoringForm(); break;
                default: f = new DashboardForm(); break;
            }
            f.TopLevel = false; f.FormBorderStyle = FormBorderStyle.None; f.Dock = DockStyle.Fill;
            _host.Controls.Add(f); f.Show();
        }
    }
}