using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class AuditHistoryForm : Form
    {
        private readonly DateTimePicker from = new DateTimePicker { Width=145,Format=DateTimePickerFormat.Short };
        private readonly DateTimePicker until = new DateTimePicker { Width=145,Format=DateTimePickerFormat.Short };
        private readonly TextBox user = new TextBox { Width=120 };
        private readonly TextBox action = new TextBox { Width=160 };
        private readonly DataGridView grid = new DataGridView {
            Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,
            AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,RowHeadersVisible=false
        };
        private readonly Label status = new Label { Dock=DockStyle.Bottom,Height=35,Padding=new Padding(8) };
        public AuditHistoryForm()
        {
            if (!AuthorizationService.CanAccess("auditlog"))
                throw new UnauthorizedAccessException("Administrator access required.");
            bool fa=LocalizationService.IsPersian;
            Text=fa ? "تاریخچه فعالیت کاربران" : "User activity audit history";
            RightToLeft=fa ? RightToLeft.Yes : RightToLeft.No;
            BackColor=Color.FromArgb(242,246,252);
            from.Value=DateTime.Today.AddDays(-30);
            until.Value=DateTime.Today;
            var bar=new FlowLayoutPanel { Dock=DockStyle.Top,Height=98,Padding=new Padding(8),AutoScroll=true };
            bar.Controls.Add(new Label { Text=fa?"از تاریخ":"From",AutoSize=true,Padding=new Padding(0,8,0,0) });bar.Controls.Add(from);
            bar.Controls.Add(new Label { Text=fa?"تا تاریخ":"To",AutoSize=true,Padding=new Padding(0,8,0,0) });bar.Controls.Add(until);
            bar.Controls.Add(new Label { Text=fa?"کاربر":"User",AutoSize=true,Padding=new Padding(0,8,0,0) });bar.Controls.Add(user);
            bar.Controls.Add(new Label { Text=fa?"عملیات":"Action",AutoSize=true,Padding=new Padding(0,8,0,0) });bar.Controls.Add(action);
            var run=new Button { Text=fa?"جستجو":"Search",AutoSize=true,BackColor=Color.FromArgb(36,96,170),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            run.Click+=(s,e)=>RefreshRows();bar.Controls.Add(run);
            var csv=new Button { Text=fa?"خروجی CSV":"Export CSV",AutoSize=true,BackColor=Color.FromArgb(27,126,103),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            csv.Click+=(s,e)=>ReportExportService.SaveCsv(grid,"AuditHistory");bar.Controls.Add(csv);
            Controls.Add(grid);Controls.Add(status);Controls.Add(bar);
            Shown+=(s,e)=>RefreshRows();
        }
        private void RefreshRows()
        {
            try
            {
                // Stored timestamps are UTC. Date filters are UTC calendar dates.
                var data=AuditLogQueryService.Search(from.Value.Date,until.Value.Date.AddDays(1),user.Text,action.Text,500);
                grid.DataSource=data;
                status.Text=(LocalizationService.IsPersian?"تعداد ردیف‌های نمایش‌داده‌شده (حداکثر ۵۰۰): ":"Displayed rows (max 500): ")+data.Rows.Count;
            }
            catch(Exception ex)
            {
                grid.DataSource=null;status.Text=LocalizationService.IsPersian?"جستجو ناموفق بود":"Search failed";
                MessageBox.Show(ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
    }
}
