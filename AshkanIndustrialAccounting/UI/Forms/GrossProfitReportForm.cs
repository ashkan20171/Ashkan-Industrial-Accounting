using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class GrossProfitReportForm : Form
    {
        private readonly DateTimePicker from = new DateTimePicker { Width=130, Format=DateTimePickerFormat.Short };
        private readonly DateTimePicker until = new DateTimePicker { Width=130, Format=DateTimePickerFormat.Short };
        private readonly DataGridView grid = new DataGridView();
        private readonly Label summary = new Label();
        public GrossProfitReportForm()
        {
            bool fa=LocalizationService.IsPersian;
            Text=fa ? "گزارش سود ناخالص فروش" : "Sales gross profit report";
            RightToLeft=fa ? RightToLeft.Yes : RightToLeft.No;
            BackColor=Color.FromArgb(245,248,252);
            from.Value=DateTime.Today.AddMonths(-1); until.Value=DateTime.Today;
            var toolbar=new FlowLayoutPanel { Dock=DockStyle.Top,Height=56,Padding=new Padding(8) };
            toolbar.Controls.Add(new Label { Text=fa?"از تاریخ":"From",AutoSize=true,Margin=new Padding(8,9,4,0) });
            toolbar.Controls.Add(from);
            toolbar.Controls.Add(new Label { Text=fa?"تا تاریخ":"To",AutoSize=true,Margin=new Padding(8,9,4,0) });
            toolbar.Controls.Add(until);
            var refresh=new Button { Text=fa?"نمایش گزارش":"Run report",AutoSize=true,BackColor=Color.FromArgb(35,99,175),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            refresh.Click+=(sender,args)=>LoadData();toolbar.Controls.Add(refresh);
            var export = new Button { Text=LocalizationService.IsPersian ? "خروجی Excel (CSV)" : "Export Excel (CSV)",
                AutoSize=true,BackColor=Color.FromArgb(34,105,96),ForeColor=Color.White,
                FlatStyle=FlatStyle.Flat };
            export.Click+=(sender,args)=>ReportExportService.SaveCsv(grid, "GrossProfitReport");
            toolbar.Controls.Add(export);
            var print = new Button {
                Text=LocalizationService.IsPersian ? "چاپ / ذخیره PDF" : "Print / Save PDF",
                AutoSize=true,BackColor=Color.FromArgb(72,79,140),ForeColor=Color.White,
                FlatStyle=FlatStyle.Flat };
            print.Click+=(sender,args)=>ReportPrintService.Print(grid,Text);
            toolbar.Controls.Add(print);
            grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.AllowUserToAddRows=false;
            grid.AllowUserToDeleteRows=false;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor=Color.White;grid.RowHeadersVisible=false;
            summary.Dock=DockStyle.Bottom;summary.Height=54;summary.Padding=new Padding(12);
            summary.Font=new Font("Segoe UI",10,FontStyle.Bold);
            Controls.Add(grid);Controls.Add(summary);Controls.Add(toolbar);
            Shown+=(sender,args)=>LoadData();
        }
        private void LoadData()
        {
            try
            {
                DataTable data=GrossProfitReportService.Read(from.Value,until.Value);
                int uncosted=GrossProfitReportService.CountUncosted(from.Value,until.Value);
                grid.DataSource=data;
                decimal revenue=0m,cost=0m;
                string currency=null;bool mixed=false;
                foreach(DataRow row in data.Rows)
                {
                    string current=Convert.ToString(row["CurrencyCode"]);
                    if(currency!=null && currency!=current)mixed=true;
                    currency=current;
                    revenue+=Convert.ToDecimal(row["Revenue"]);
                    cost+=Convert.ToDecimal(row["CostOfGoodsSold"]);
                }
                bool fa=LocalizationService.IsPersian;
                summary.Text=(fa?"فاکتورهای محاسبه‌شده: ":"Costed invoices: ")+data.Rows.Count+
                    (fa?" | بدون بهای ثبت‌شده: ":" | Missing cost: ")+uncosted+
                    (mixed?(fa?" | چندارزی: جمع‌زدن غیرفعال است":" | Mixed currencies: totals suppressed"):
                    (fa?" | درآمد: ":" | Revenue: ")+revenue.ToString("N2")+
                    (fa?" | بهای تمام‌شده: ":" | COGS: ")+cost.ToString("N2")+
                    (fa?" | سود ناخالص: ":" | Gross profit: ")+(revenue-cost).ToString("N2")+
                    " "+currency);
            }
            catch(Exception ex) { MessageBox.Show(ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error); }
        }
    }
}
