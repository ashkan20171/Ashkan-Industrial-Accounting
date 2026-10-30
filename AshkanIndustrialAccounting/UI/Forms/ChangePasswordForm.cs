using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;
namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class ChangePasswordForm : Form
    {
        public ChangePasswordForm()
        {
            bool fa = LocalizationService.IsPersian;
            Text = fa ? "تغییر رمز عبور" : "Change password";
            Width = 440; Height = 330; StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 247, 251); Font = new Font("Segoe UI", 10);
            RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 7, AutoScroll = true };
            var current = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Top };
            var next = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Top };
            var confirm = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Top };
            layout.Controls.Add(new Label { Text = fa ? "رمز فعلی" : "Current password", AutoSize = true }); layout.Controls.Add(current);
            layout.Controls.Add(new Label { Text = fa ? "رمز جدید" : "New password", AutoSize = true }); layout.Controls.Add(next);
            layout.Controls.Add(new Label { Text = fa ? "تکرار رمز جدید" : "Confirm new password", AutoSize = true }); layout.Controls.Add(confirm);
            var save = new Button { Text = fa ? "ذخیره" : "Save", Height = 38, Dock = DockStyle.Top, BackColor = Color.FromArgb(25, 99, 180), ForeColor = Color.White };
            save.Click += (s, e) => {
                if (next.Text != confirm.Text) { MessageBox.Show(fa ? "رمزهای جدید یکسان نیستند." : "New passwords do not match."); return; }
                try { SqlAuthenticationService.ChangePassword(AuthorizationService.CurrentUserName, current.Text, next.Text); MessageBox.Show(fa ? "رمز تغییر کرد." : "Password changed."); DialogResult = DialogResult.OK; }
                catch (Exception ex) { MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            layout.Controls.Add(save); Controls.Add(layout); AcceptButton = save;
        }
    }
}
