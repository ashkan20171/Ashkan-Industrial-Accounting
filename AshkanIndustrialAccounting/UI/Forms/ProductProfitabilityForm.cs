using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class ProductProfitabilityForm : Form
    {
        private readonly DateTimePicker from=new DateTimePicker { Width=130,Format=DateTimePickerFormat.Short };
        private readonly DateTimePicker to=new DateTimePicker { Width=130,Format=DateTimePickerFormat.Short };
        private readonly DataGridView grid=new DataGridView();
        private readonly Label footer=new Label();
        public ProductProfitabilityForm()
        {
            bool fa=LocalizationService.IsPersian;
            Text=fa?"تحلیل سودآوری محصولات":"Product profitability analysis";
            RightToLeft=fa?RightToLeft.Yes:RightToLeft.No;
            BackColor=Color.FromArgb(245,248,252);
            from.Value=DateTime.Today.AddMonths(-1);to.Value=DateTime.Today;
            var bar=new FlowLayoutPanel { Dock=DockStyle.Top,Height=60,Padding=new Padding(10) };
            bar.Controls.Add(new Label { Text=fa?"از تاریخ":"From",AutoSize=true,Margin=new Padding(6,8,4,0) });
            bar.Controls.Add(from);
            bar.Controls.Add(new Label { Text=fa?"تا تاریخ":"To",AutoSize=true,Margin=new Padding(6,8,4,0) });
            bar.Controls.Add(to);
            var run=new Button { Text=fa?"به‌روزرسانی":"Refresh",AutoSize=true,
                BackColor=Color.FromArgb(22,124,105),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            run.Click+=(sender,args)=>RefreshReport();bar.Controls.Add(run);
            var export = new Button { Text=LocalizationService.IsPersian ? "خروجی Excel (CSV)" : "Export Excel (CSV)",
                AutoSize=true,BackColor=Color.FromArgb(34,105,96),ForeColor=Color.White,
                FlatStyle=FlatStyle.Flat };
            export.Click+=(sender,args)=>ReportExportService.SaveCsv(grid, "ProductProfitability");
            bar.Controls.Add(export);
            var print = new Button {
                Text=LocalizationService.IsPersian ? "چاپ / ذخیره PDF" : "Print / Save PDF",
                AutoSize=true,BackColor=Color.FromArgb(72,79,140),ForeColor=Color.White,
                FlatStyle=FlatStyle.Flat };
            print.Click+=(sender,args)=>ReportPrintService.Print(grid,Text);
            bar.Controls.Add(print);
            grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.AllowUserToAddRows=false;
            grid.AllowUserToDeleteRows=false;grid.RowHeadersVisible=false;
            grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor=Color.White;
            footer.Dock=DockStyle.Bottom;footer.Height=52;footer.Padding=new Padding(12);
            footer.Font=new Font("Segoe UI",10,FontStyle.Bold);
            Controls.Add(grid);Controls.Add(footer);Controls.Add(bar);
            Shown+=(sender,args)=>RefreshReport();
        }
        private void RefreshReport()
        {
            try
            {
                int excluded;
                DataTable table=ProductProfitabilityService.Read(from.Value,to.Value,out excluded);
                grid.DataSource=table;
                bool fa=LocalizationService.IsPersian;
                var currencies=new System.Collections.Generic.HashSet<string>();
                decimal revenue=0m,cost=0m;
                foreach(DataRow row in table.Rows)
                {
                    currencies.Add(Convert.ToString(row["Currency"]));
                    revenue+=Convert.ToDecimal(row["NetRevenue"]);
                    cost+=Convert.ToDecimal(row["COGS"]);
                }
                footer.Text=(fa?"محصولات: ":"Products: ")+table.Rows.Count+
                    (fa?" | فاکتورهای کنارگذاشته‌شده: ":" | Excluded invoices: ")+excluded;
                if(currencies.Count==1)
                    footer.Text+=(fa?" | درآمد خالص: ":" | Net sales: ")+revenue.ToString("N2")+
                        (fa?" | سود ناخالص: ":" | Gross profit: ")+(revenue-cost).ToString("N2");
                else if(currencies.Count>1)
                    footer.Text+=fa?" | چندارزی: جمع کل نمایش داده نمی‌شود":
                        " | Mixed currencies: totals suppressed";
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
    }
}
