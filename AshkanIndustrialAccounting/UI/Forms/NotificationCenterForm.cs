using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class NotificationCenterForm : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly CheckBox _unreadOnly = new CheckBox();
        private readonly Label _summary = new Label();
        private readonly Button _refresh = new Button();
        private readonly Button _markRead = new Button();
        private readonly Button _markAll = new Button();
        private readonly Button _preferences = new Button();

        public NotificationCenterForm()
        {
            Dock = DockStyle.Fill; BackColor = Color.FromArgb(242, 246, 251); Padding = new Padding(18);
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, FlowDirection = LocalizationService.IsPersian ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, WrapContents = false };
            StyleButton(_refresh, LocalizationService.IsPersian ? "بروزرسانی" : "Refresh");
            StyleButton(_markRead, LocalizationService.IsPersian ? "خوانده شد" : "Mark as read");
            StyleButton(_markAll, LocalizationService.IsPersian ? "خواندن همه" : "Mark all read");
            StyleButton(_preferences, LocalizationService.IsPersian ? "تنظیمات اعلان" : "Preferences");
            _preferences.Click += (s,e) => EditPreferences();
            _unreadOnly.Text = LocalizationService.IsPersian ? "فقط خوانده‌نشده‌ها" : "Unread only";
            _unreadOnly.AutoSize = true; _unreadOnly.Margin = new Padding(12, 12, 12, 0);
            _refresh.Click += (s,e) => LoadInbox(); _markRead.Click += (s,e) => MarkSelected();
            _markAll.Click += (s,e) => MarkAll(); _unreadOnly.CheckedChanged += (s,e) => LoadInbox();
            toolbar.Controls.Add(_refresh); toolbar.Controls.Add(_markRead); toolbar.Controls.Add(_markAll); toolbar.Controls.Add(_preferences); toolbar.Controls.Add(_unreadOnly);
            _summary.Dock = DockStyle.Top; _summary.Height = 36; _summary.TextAlign = ContentAlignment.MiddleLeft; _summary.ForeColor = Color.FromArgb(65,82,104);
            _grid.Dock = DockStyle.Fill; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false; _grid.RowHeadersVisible = false;
            _grid.CellDoubleClick += (s, e) => OpenRelatedAlert(); _grid.BackgroundColor = Color.White; _grid.BorderStyle = BorderStyle.None;
            _grid.EnableHeadersVisualStyles = false; _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(226,235,246); _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(28,48,72);
            Controls.Add(_grid); Controls.Add(_summary); Controls.Add(toolbar);
            RightToLeft = LocalizationService.IsPersian ? RightToLeft.Yes : RightToLeft.No; RightToLeftLayout = LocalizationService.IsPersian;
            Load += (s,e) => LoadInbox();
        }

        private static void StyleButton(Button b, string text)
        { b.Text=text; b.Width=125; b.Height=36; b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderSize=0; b.BackColor=Color.FromArgb(25,99,180); b.ForeColor=Color.White; b.Cursor=Cursors.Hand; b.Margin=new Padding(4); }

        private void LoadInbox()
        {
            try
            {
                _grid.DataSource = NotificationInboxService.GetInbox(_unreadOnly.Checked);
                if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
                if (_grid.Columns.Contains("AlertId")) _grid.Columns["AlertId"].Visible = false;
                if (_grid.Columns.Contains("Title")) _grid.Columns["Title"].HeaderText = LocalizationService.IsPersian ? "عنوان" : "Title";
                if (_grid.Columns.Contains("Message")) _grid.Columns["Message"].HeaderText = LocalizationService.IsPersian ? "پیام" : "Message";
                if (_grid.Columns.Contains("SeverityCode")) _grid.Columns["SeverityCode"].HeaderText = LocalizationService.IsPersian ? "شدت" : "Severity";
                if (_grid.Columns.Contains("CreatedAtUtc")) _grid.Columns["CreatedAtUtc"].HeaderText = LocalizationService.IsPersian ? "زمان ثبت (UTC)" : "Created (UTC)";
                if (_grid.Columns.Contains("ReadAtUtc")) _grid.Columns["ReadAtUtc"].HeaderText = LocalizationService.IsPersian ? "زمان مطالعه (UTC)" : "Read (UTC)";
                int count = NotificationInboxService.GetUnreadCount();
                _summary.Text = LocalizationService.IsPersian ? "اعلان‌های خوانده‌نشده: " + count : "Unread notifications: " + count;
            }
            catch (Exception ex)
            {
                MessageBox.Show((LocalizationService.IsPersian ? "بارگذاری اعلان‌ها انجام نشد:\n" : "Could not load notifications:\n") + ex.Message,
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private void OpenRelatedAlert()
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("AlertId")) return;
            if (!AuthorizationService.CanAccess("cashbank"))
            {
                MessageBox.Show(LocalizationService.IsPersian ? "برای مشاهده جزئیات هشدار بانکی، دسترسی بخش خزانه‌داری لازم است." : "Cash/bank access is required to view the related alert.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                long notificationId = Convert.ToInt64(_grid.CurrentRow.Cells["Id"].Value);
                int alertId = Convert.ToInt32(_grid.CurrentRow.Cells["AlertId"].Value);
                NotificationInboxService.MarkAsRead(notificationId);
                var shell = FindForm() as MainForm;
                if (shell != null) shell.NavigateToFeature("cashbank");
                MessageBox.Show(LocalizationService.IsPersian
                    ? "به بخش خزانه‌داری منتقل شدید. از گزینه «هشدارهای بانکی» برای مشاهده هشدار مرتبط استفاده کنید. شناسه هشدار: " + alertId
                    : "Opened Cash & Bank. Use Bank Alerts to inspect the related alert. Alert ID: " + alertId,
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadInbox();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }


        private void EditPreferences()
        {
            try
            {
                var current = NotificationInboxService.GetPreferences();
                bool fa = LocalizationService.IsPersian;
                using (var dialog = new Form { Text = fa ? "تنظیمات اعلان‌ها" : "Notification preferences", Width = 390, Height = 330,
                    StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false, MinimizeBox = false, RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No,
                    RightToLeftLayout = fa, BackColor = Color.White, Padding = new Padding(18) })
                {
                    var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
                    var enabled = new CheckBox { Text = fa ? "فعال بودن اعلان‌های داخل برنامه" : "Enable in-app notifications", AutoSize = true, Checked = current.EnableInApp, Margin = new Padding(8) };
                    var critical = PreferenceCheck(fa ? "بحرانی" : "Critical", current.NotifyCritical);
                    var high = PreferenceCheck(fa ? "زیاد" : "High", current.NotifyHigh);
                    var medium = PreferenceCheck(fa ? "متوسط" : "Medium", current.NotifyMedium);
                    var low = PreferenceCheck(fa ? "کم" : "Low", current.NotifyLow);
                    var save = new Button { Text = fa ? "ذخیره تنظیمات" : "Save preferences", Width = 150, Height = 36, BackColor = Color.FromArgb(25,99,180), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(8) };
                    save.Click += (s,e) => { try { NotificationInboxService.SavePreferences(new NotificationInboxService.NotificationPreferences {
                        EnableInApp = enabled.Checked, NotifyCritical = critical.Checked, NotifyHigh = high.Checked,
                        NotifyMedium = medium.Checked, NotifyLow = low.Checked }); dialog.DialogResult = DialogResult.OK; dialog.Close(); }
                        catch (Exception ex) { MessageBox.Show(ex.Message, dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); } };
                    layout.Controls.Add(enabled); layout.Controls.Add(critical); layout.Controls.Add(high); layout.Controls.Add(medium); layout.Controls.Add(low); layout.Controls.Add(save);
                    dialog.Controls.Add(layout); dialog.ShowDialog(this);
                    LoadInbox();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private static CheckBox PreferenceCheck(string text, bool value)
        { return new CheckBox { Text = text, Checked = value, AutoSize = true, Margin = new Padding(8) }; }

        private void MarkSelected()
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id")) return;
            try { NotificationInboxService.MarkAsRead(Convert.ToInt64(_grid.CurrentRow.Cells["Id"].Value)); LoadInbox(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void MarkAll()
        {
            try
            {
                int count = NotificationInboxService.MarkAllAsRead(); LoadInbox();
                MessageBox.Show(LocalizationService.IsPersian ? count + " اعلان خوانده شد." : count + " notifications marked as read.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}
