using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;
using AshkanIndustrialAccounting.UI.Controls;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class DashboardForm : Form
    {
        private readonly FlowLayoutPanel _cards = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 155, AutoScroll = true, WrapContents = false, Padding = new Padding(0, 8, 0, 8) };
        private readonly Label _status = new Label { Dock = DockStyle.Top, Height = 42, Padding = new Padding(12, 10, 4, 0), BackColor = Color.FromArgb(230, 241, 251), ForeColor = Color.FromArgb(30, 72, 115) };
        private readonly Panel _chart = new Panel { Dock = DockStyle.Top, Height = 190, BackColor = Color.White, Padding = new Padding(16) };
        private decimal _debit, _credit;
        private readonly Label _details = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11), Padding = new Padding(20), ForeColor = Color.FromArgb(56, 73, 92) };

        public DashboardForm()
        {
            BackColor = Color.FromArgb(242, 246, 251);
            var refresh = new Button { Text = LocalizationService.IsPersian ? "↻ بروزرسانی اطلاعات" : "↻ Refresh live data", Dock = DockStyle.Top, Height = 42, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(25, 99, 180), ForeColor = Color.White };
            refresh.FlatAppearance.BorderSize = 0;
            refresh.Click += (s, e) => RefreshData();
            var auditButton = new Button {
                Text = LocalizationService.IsPersian ? "بررسی مغایرت‌های مالی و انبار" : "Review financial & inventory issues",
                Dock = DockStyle.Top, Height = 42, FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(151, 79, 54), ForeColor = Color.White
            };
            auditButton.FlatAppearance.BorderSize = 0;
            auditButton.Click += (s, e) => {
                using (var dialog = new FinancialIntegrityAuditForm()) {
                    dialog.Size = new Size(1100, 680);
                    dialog.StartPosition = FormStartPosition.CenterParent;
                    dialog.ShowDialog(this);
                }
                RefreshData();
            };
            var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14) };
            content.Controls.Add(_details);
            content.Controls.Add(_chart);
            _chart.Paint += DrawChart;
            Controls.Add(content);
            Controls.Add(_cards);
            Controls.Add(_status);
            Controls.Add(auditButton);
            Controls.Add(refresh);
            Shown += (s, e) => RefreshData();
        }

        private void RefreshData()
        {
            _cards.Controls.Clear();
            try
            {
                using (var connection = Db.Open())
                using (var command = new SqlCommand(@"
SELECT
 (SELECT COUNT(*) FROM dbo.JournalEntries WHERE IsPosted = 1),
 (SELECT COUNT(*) FROM dbo.JournalEntries WHERE IsPosted = 0),
 (SELECT ISNULL(SUM(l.Debit), 0) FROM dbo.JournalLines l INNER JOIN dbo.JournalEntries e ON e.Id = l.JournalEntryId WHERE e.IsPosted = 1),
 (SELECT ISNULL(SUM(l.Credit), 0) FROM dbo.JournalLines l INNER JOIN dbo.JournalEntries e ON e.Id = l.JournalEntryId WHERE e.IsPosted = 1),
 (SELECT COUNT(*) FROM dbo.Accounts WHERE IsActive = 1),
 (SELECT COUNT(*) FROM dbo.CostCenters WHERE IsActive = 1)", connection))
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) throw new InvalidOperationException("No accounting data returned.");
                    var fa = LocalizationService.IsPersian;
                    Add(fa ? "اسناد قطعی" : "Posted entries", reader.GetInt32(0).ToString("N0"));
                    Add(fa ? "اسناد پیش‌نویس" : "Draft entries", reader.GetInt32(1).ToString("N0"));
                    var debit = reader.GetDecimal(2);
                    var credit = reader.GetDecimal(3);
                    _debit = debit; _credit = credit; _chart.Invalidate();
                    Add(fa ? "گردش بدهکار" : "Posted debits", CurrencyService.Format(debit));
                    Add(fa ? "گردش بستانکار" : "Posted credits", CurrencyService.Format(credit));
                    Add(fa ? "حساب‌های فعال" : "Active accounts", reader.GetInt32(4).ToString("N0"));
                    Add(fa ? "مراکز هزینه" : "Cost centers", reader.GetInt32(5).ToString("N0"));
                    _details.Text = (fa ? "وضعیت تراز اسناد قطعی: " : "Posted ledger balance: ") +
                        (debit == credit ? (fa ? "متوازن ✓" : "Balanced ✓") : (fa ? "نامتوازن ⚠" : "Unbalanced ⚠")) +
                        "\n" + (fa ? "شاخص‌های مدیریتی فقط از داده‌های ثبت‌شده محاسبه می‌شوند." :
                        "Management metrics use recorded data only.");
                    // Keep the original ledger cards even if a newer operational table
                    // has not yet been migrated. A failed KPI is never shown as zero.
                    AppendManagementMetrics(fa);
                    AppendAuditSummary(fa);
                    _status.Text = (fa ? "داده‌های واقعی • آخرین بروزرسانی: " : "LIVE SQL DATA • Updated: ") + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            catch (Exception ex)
            {
                _status.Text = LocalizationService.IsPersian ? "اتصال به دیتابیس ناموفق بود؛ داده ساختگی نمایش داده نمی‌شود." : "Database unavailable; no fabricated metrics are displayed.";
                _debit = _credit = 0; _chart.Invalidate();
                _details.Text = ex.Message;
            }
        }
        private void AppendManagementMetrics(bool fa)
        {
            var warnings = new System.Collections.Generic.List<string>();
            try
            {
                var inventory = ManagementKpiService.InventorySummary();
                if (inventory.Rows.Count > 0)
                {
                    var row = inventory.Rows[0];
                    Add(fa ? "ارزش ثبت‌شده انبار" : "Recorded inventory value",
                        CurrencyService.Format(Convert.ToDecimal(row["TotalInventoryValue"])));
                    int differences = Convert.ToInt32(row["ReconciliationAlerts"]);
                    Add(fa ? "مغایرت موجودی" : "Inventory discrepancies", differences.ToString("N0"));
                    int negative = Convert.ToInt32(row["NegativeStockProducts"]);
                    if (differences > 0 || negative > 0)
                        warnings.Add((fa ? "مغایرت انبار: " : "Inventory mismatches: ") + differences +
                            (fa ? " | موجودی منفی: " : " | Negative stock: ") + negative);
                }
            }
            catch (Exception ex)
            {
                warnings.Add((fa ? "گزارش انبار در دسترس نیست: " : "Inventory KPIs unavailable: ") + ex.Message);
            }
            try
            {
                int unbalanced = ManagementKpiService.UnbalancedPostedEntries();
                Add(fa ? "اسناد نامتوازن" : "Unbalanced entries", unbalanced.ToString("N0"));
                if (unbalanced > 0)
                    warnings.Add((fa ? "اسناد قطعی نامتوازن: " : "Unbalanced posted entries: ") + unbalanced);
            }
            catch (Exception ex)
            {
                warnings.Add((fa ? "کنترل اسناد در دسترس نیست: " : "Ledger audit unavailable: ") + ex.Message);
            }
            try
            {
                var sales = ManagementKpiService.SalesByCurrency(DateTime.Today.AddDays(-29), DateTime.Today);
                foreach (System.Data.DataRow row in sales.Rows)
                    Add((fa ? "فروش خالص ۳۰ روز • " : "30-day net sales • ") +
                        Convert.ToString(row["CurrencyCode"]),
                        Convert.ToDecimal(row["NetSales"]).ToString("N2"));
                if (sales.Rows.Count == 0)
                    warnings.Add(fa ? "فروش قطعی در ۳۰ روز اخیر ثبت نشده است." :
                        "No posted sales in the last 30 days.");
            }
            catch (Exception ex)
            {
                warnings.Add((fa ? "شاخص فروش در دسترس نیست: " : "Sales KPIs unavailable: ") + ex.Message);
            }
            if (warnings.Count > 0)
                _details.Text += "\n\n" + string.Join("\n", warnings);
        }

        private void AppendAuditSummary(bool fa)
        {
            try
            {
                var issues = FinancialIntegrityAuditService.Run();
                int count = issues.Rows.Count;
                Add(fa ? "هشدارهای ممیزی" : "Integrity alerts", count.ToString("N0"));
                if (count == 0)
                {
                    _details.Text += fa ? "\nکنترل‌های ممیزی تعریف‌شده مغایرتی پیدا نکردند." :
                        "\nNo issues found by the configured integrity checks.";
                }
                else
                {
                    var byType = new System.Collections.Generic.SortedDictionary<string, int>();
                    foreach (System.Data.DataRow issue in issues.Rows)
                    {
                        string key = Convert.ToString(issue["IssueCode"]);
                        byType[key] = byType.ContainsKey(key) ? byType[key] + 1 : 1;
                    }
                    _details.Text += fa ? "\n\nهشدارهای ممیزی (برای جزئیات دکمه بالا را بزنید):" :
                        "\n\nIntegrity alerts (open audit for details):";
                    foreach (var entry in byType)
                        _details.Text += "\n" + entry.Key + ": " + entry.Value;
                }
            }
            catch (Exception ex)
            {
                _details.Text += (fa ? "\nممیزی قابل اجرا نیست: " : "\nIntegrity audit unavailable: ") + ex.Message;
            }
        }

        private void DrawChart(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            bool fa = LocalizationService.IsPersian;
            using (var titleBrush = new SolidBrush(Color.FromArgb(32, 51, 76)))
            using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
                g.DrawString(fa ? "مقایسه گردش اسناد قطعی" : "Posted journal turnover", font, titleBrush, 14, 8);
            decimal max = Math.Max(_debit, _credit);
            if (max <= 0) return;
            var usable = Math.Max(40, _chart.ClientSize.Width - 210);
            var rows = new[] { new { Name = fa ? "بدهکار" : "Debit", Value = _debit, Color = Color.FromArgb(25, 99, 180) }, new { Name = fa ? "بستانکار" : "Credit", Value = _credit, Color = Color.FromArgb(24, 153, 126) } };
            for (int i = 0; i < rows.Length; i++)
            {
                int y = 55 + i * 55;
                using (var font = new Font("Segoe UI", 10))
                using (var labelBrush = new SolidBrush(Color.FromArgb(50, 65, 82)))
                    g.DrawString(rows[i].Name, font, labelBrush, 14, y);
                int width = (int)(usable * (rows[i].Value / max));
                using (var brush = new SolidBrush(rows[i].Color)) g.FillRectangle(brush, 100, y, width, 25);
            }
        }
        private void Add(string title, string value) { var card = new MetricCard(); card.SetMetric(title, value); _cards.Controls.Add(card); }
    }
}
