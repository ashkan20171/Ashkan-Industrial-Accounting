using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;
namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class SettingsForm : Form
    {
        public SettingsForm()
        {
            BackColor=Color.FromArgb(245,248,252);
            var title=new Label{Text=LocalizationService.T("settings"),Dock=DockStyle.Top,Height=58,Font=new Font("Segoe UI Semibold",22)};
            var info=new Label{Dock=DockStyle.Top,Height=180,Font=new Font("Segoe UI",11),Text=LocalizationService.IsPersian?
@"تنظیمات سازمان
• زبان و جهت رابط کاربری
• سال مالی و دوره‌ها
• شماره‌گذاری اسناد
• سیاست‌های امنیتی و نقش‌ها
• پشتیبان‌گیری و ثبت رویداد
• تنظیمات مالیات و ارز":
@"Organization settings
• Language and UI direction
• Fiscal year and periods
• Journal numbering
• Security policies and roles
• Backup and audit logging
• Tax and currency configuration"};
            var change = new Button { Text = LocalizationService.IsPersian ? "تغییر رمز عبور SQL" : "Change SQL password", Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(25,99,180), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            change.Click += (sender, args) => { using (var dialog = new ChangePasswordForm()) dialog.ShowDialog(this); };
            Controls.Add(change); Controls.Add(info);Controls.Add(title);
        }
    }
}
