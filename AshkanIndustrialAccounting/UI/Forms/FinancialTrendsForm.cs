using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class FinancialTrendsForm : Form
    {
        private readonly DateTimePicker from=new DateTimePicker { Format=DateTimePickerFormat.Short,Width=125 };
        private readonly DateTimePicker to=new DateTimePicker { Format=DateTimePickerFormat.Short,Width=125 };
        private readonly DataGridView table=new DataGridView();
        private readonly Panel chart=new Panel { Dock=DockStyle.Top,Height=245,BackColor=Color.White };
        private readonly Label notice=new Label { Dock=DockStyle.Bottom,Height=50,Padding=new Padding(10) };
        private DataTable data;
        public FinancialTrendsForm()
        {
            bool fa=LocalizationService.IsPersian;
            Text=fa?"روند ماهانه فروش و سود":"Monthly sales and profit trend";
            RightToLeft=fa?RightToLeft.Yes:RightToLeft.No;
            BackColor=Color.FromArgb(242,246,251);
            from.Value=DateTime.Today.AddMonths(-11);to.Value=DateTime.Today;
            var bar=new FlowLayoutPanel { Dock=DockStyle.Top,Height=55,Padding=new Padding(8) };
            bar.Controls.Add(new Label { Text=fa?"از":"From",AutoSize=true,Margin=new Padding(6,8,3,0) });
            bar.Controls.Add(from);
            bar.Controls.Add(new Label { Text=fa?"تا":"To",AutoSize=true,Margin=new Padding(6,8,3,0) });
            bar.Controls.Add(to);
            var refresh=new Button { Text=fa?"نمایش":"Refresh",AutoSize=true,BackColor=Color.FromArgb(20,119,105),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            refresh.Click+=(s,e)=>LoadData();bar.Controls.Add(refresh);
            var export = new Button { Text=LocalizationService.IsPersian ? "خروجی Excel (CSV)" : "Export Excel (CSV)",
                AutoSize=true,BackColor=Color.FromArgb(34,105,96),ForeColor=Color.White,
                FlatStyle=FlatStyle.Flat };
            export.Click+=(sender,args)=>ReportExportService.SaveCsv(table, "FinancialTrends");
            bar.Controls.Add(export);
            var print = new Button {
                Text=LocalizationService.IsPersian ? "چاپ / ذخیره PDF" : "Print / Save PDF",
                AutoSize=true,BackColor=Color.FromArgb(72,79,140),ForeColor=Color.White,
                FlatStyle=FlatStyle.Flat };
            print.Click+=(sender,args)=>ReportPrintService.Print(table,Text);
            bar.Controls.Add(print);
            table.Dock=DockStyle.Fill;table.ReadOnly=true;table.AllowUserToAddRows=false;
            table.AllowUserToDeleteRows=false;table.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
            table.BackgroundColor=Color.White;table.RowHeadersVisible=false;
            chart.Paint+=DrawBars;
            Controls.Add(table);Controls.Add(chart);Controls.Add(notice);Controls.Add(bar);
            Shown+=(s,e)=>LoadData();
        }
        private void LoadData()
        {
            try
            {
                data=FinancialTrendsService.Read(from.Value,to.Value);
                table.DataSource=data;
                var currencies=new System.Collections.Generic.HashSet<string>();
                foreach(DataRow r in data.Rows) currencies.Add(Convert.ToString(r["CurrencyCode"]));
                bool fa=LocalizationService.IsPersian;
                notice.Text=(fa?"ماه/ارز ثبت‌شده: ":"Month/currency rows: ")+data.Rows.Count+
                    (currencies.Count>1?(fa?" | چندارزی: هر ردیف جداگانه نمایش داده می‌شود":
                    " | Multiple currencies: rows are not aggregated"):"");
                chart.Invalidate();
            }
            catch(Exception ex)
            {
                data=null;table.DataSource=null;chart.Invalidate();
                MessageBox.Show(ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
        private void DrawBars(object sender,PaintEventArgs e)
        {
            var g=e.Graphics;
            g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            bool fa=LocalizationService.IsPersian;
            using(var brush=new SolidBrush(Color.FromArgb(40,58,80)))
            using(var font=new Font("Segoe UI",10,FontStyle.Bold))
                g.DrawString(fa?"فروش خالص (آبی) و سود ناخالص (سبز) — ردیف‌های اخیر":
                    "Net sales (blue) and gross profit (green) — recent rows",font,brush,12,12);
            if(data==null || data.Rows.Count==0)return;
            int count=Math.Min(8,data.Rows.Count);
            int first=data.Rows.Count-count;
            decimal max=0m;
            for(int i=first;i<data.Rows.Count;i++)
            {
                var r=data.Rows[i];
                max=Math.Max(max,Math.Abs(Convert.ToDecimal(r["NetRevenue"])));
                max=Math.Max(max,Math.Abs(Convert.ToDecimal(r["GrossProfit"])));
            }
            if(max<=0m)return;
            int width=Math.Max(1,(chart.ClientSize.Width-35)/count);
            int usable=145;
            for(int i=0;i<count;i++)
            {
                var r=data.Rows[first+i];
                decimal revenue=Convert.ToDecimal(r["NetRevenue"]);
                decimal profit=Convert.ToDecimal(r["GrossProfit"]);
                int x=16+i*width;
                int h1=(int)(Math.Abs(revenue)/max*usable);
                int h2=(int)(Math.Abs(profit)/max*usable);
                using(var blue=new SolidBrush(Color.FromArgb(37,99,180)))
                using(var green=new SolidBrush(profit>=0?Color.FromArgb(20,145,115):Color.FromArgb(192,68,68)))
                {
                    g.FillRectangle(blue,x,190-h1,Math.Max(3,width/3),h1);
                    g.FillRectangle(green,x+Math.Max(3,width/3)+2,190-h2,Math.Max(3,width/3),h2);
                }
                string label=Convert.ToString(r["Year"])+"/"+Convert.ToInt32(r["Month"]).ToString("00");
                using(var font=new Font("Segoe UI",8))
                using(var ink=new SolidBrush(Color.FromArgb(75,85,100)))
                    g.DrawString(label,font,ink,x,198);
            }
        }
    }
}
