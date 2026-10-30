using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class InventoryValuationReportForm : Form
    {
        private readonly DataGridView grid = new DataGridView();
        private readonly Label summary = new Label();
        private readonly CheckBox differencesOnly = new CheckBox();
        public InventoryValuationReportForm()
        {
            bool fa = LocalizationService.IsPersian;
            Text = fa ? "گزارش ارزش و مغایرت موجودی" : "Inventory valuation & reconciliation";
            BackColor = Color.FromArgb(245,248,252);
            RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No;
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8) };
            differencesOnly.Text = fa ? "فقط مغایرت‌ها" : "Differences only";
            differencesOnly.AutoSize = true;
            differencesOnly.CheckedChanged += (s,e) => LoadData();
            var refresh = new Button { Text = fa ? "بازخوانی" : "Refresh", AutoSize = true };
            refresh.Click += (s,e) => LoadData();
            toolbar.Controls.Add(differencesOnly); toolbar.Controls.Add(refresh);
            var export = new Button { Text=LocalizationService.IsPersian ? "خروجی Excel (CSV)" : "Export Excel (CSV)",
                AutoSize=true,BackColor=Color.FromArgb(34,105,96),ForeColor=Color.White,
                FlatStyle=FlatStyle.Flat };
            export.Click+=(sender,args)=>ReportExportService.SaveCsv(grid, "InventoryValuationReport");
            toolbar.Controls.Add(export);
            var print = new Button {
                Text=LocalizationService.IsPersian ? "چاپ / ذخیره PDF" : "Print / Save PDF",
                AutoSize=true,BackColor=Color.FromArgb(72,79,140),ForeColor=Color.White,
                FlatStyle=FlatStyle.Flat };
            print.Click+=(sender,args)=>ReportPrintService.Print(grid,Text);
            toolbar.Controls.Add(print);
            summary.Dock = DockStyle.Bottom; summary.Height = 46;
            summary.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            summary.Padding = new Padding(12);
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true;
            grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = Color.White;
            Controls.Add(grid); Controls.Add(summary); Controls.Add(toolbar);
            Shown += (s,e) => LoadData();
        }
        private void LoadData()
        {
            try
            {
                var table = InventoryValuationReportService.Read(differencesOnly.Checked);
                grid.DataSource = table;
                decimal value = 0m;
                int mismatches = 0;
                foreach (DataRow row in table.Rows)
                {
                    if (row["InventoryValue"] != DBNull.Value) value += Convert.ToDecimal(row["InventoryValue"]);
                    if (Convert.ToString(row["ReconciliationStatus"]) != "OK") mismatches++;
                }
                bool fa = LocalizationService.IsPersian;
                summary.Text = (fa ? "تعداد ردیف: " : "Rows: ") + table.Rows.Count +
                    (fa ? " | مغایرت: " : " | Differences: ") + mismatches +
                    (fa ? " | ارزش ردیف‌های نمایش‌داده‌شده: " : " | Displayed inventory value: ") + value.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
