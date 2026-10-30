using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;
namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class FinancialIntegrityAuditForm : Form
    {
        private readonly DataGridView grid=new DataGridView { Dock=DockStyle.Fill,ReadOnly=true,
            AllowUserToAddRows=false,AllowUserToDeleteRows=false,
            AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor=Color.White,RowHeadersVisible=false };
        private readonly Label status=new Label { Dock=DockStyle.Bottom,Height=42,
            Padding=new Padding(12,10,0,0) };
        public FinancialIntegrityAuditForm()
        {
            bool fa=LocalizationService.IsPersian;
            Text=fa?"ممیزی یکپارچگی مالی و انبار":"Financial and inventory integrity audit";
            RightToLeft=fa?RightToLeft.Yes:RightToLeft.No;
            BackColor=Color.FromArgb(242,246,251);
            var toolbar=new FlowLayoutPanel { Dock=DockStyle.Top,Height=55,Padding=new Padding(8) };
            var refresh=new Button { Text=fa?"اجرای کنترل‌ها":"Run audit",AutoSize=true,
                BackColor=Color.FromArgb(30,105,172),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            refresh.Click+=(s,e)=>LoadAudit();
            var export=new Button { Text=fa?"خروجی CSV":"Export CSV",AutoSize=true,
                BackColor=Color.FromArgb(24,128,108),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            export.Click+=(s,e)=>ReportExportService.SaveCsv(grid,"FinancialIntegrityAudit");
            toolbar.Controls.Add(refresh);toolbar.Controls.Add(export);
            Controls.Add(grid);Controls.Add(status);Controls.Add(toolbar);
            Shown+=(s,e)=>LoadAudit();
        }
        private void LoadAudit()
        {
            try
            {
                var data=FinancialIntegrityAuditService.Run();grid.DataSource=data;
                bool fa=LocalizationService.IsPersian;
                status.ForeColor=data.Rows.Count==0?Color.FromArgb(18,122,96):Color.FromArgb(179,65,54);
                status.Text=data.Rows.Count==0?
                    (fa?"در کنترل‌های تعریف‌شده مغایرتی پیدا نشد.":"No issues found by configured checks."):
                    (fa?"تعداد هشدارها: ":"Issues detected: ")+data.Rows.Count;
            }
            catch(Exception ex)
            {
                grid.DataSource=null;
                status.Text=LocalizationService.IsPersian?"کنترل‌ها اجرا نشدند.":"Audit could not run.";
                MessageBox.Show(ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
    }
}
