using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class ServiceMonitoringForm : Form
    {
        private readonly DataGridView _workers = new DataGridView();
        private readonly DataGridView _queues = new DataGridView();
        private readonly DataGridView _history = new DataGridView();
        private readonly Label _title = new Label();
        private readonly Label _workerTitle = new Label();
        private readonly Label _queueTitle = new Label();
        private readonly Label _historyTitle = new Label();
        private readonly Button _refresh = new Button();
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
        public ServiceMonitoringForm()
        {
            BackColor=Color.FromArgb(242,246,251); Font=new Font("Segoe UI",9.5F);
            _title.Dock=DockStyle.Top; _title.Height=55; _title.Font=new Font("Segoe UI Semibold",16F); _title.ForeColor=Color.FromArgb(22,43,70);
            _refresh.Dock=DockStyle.Top; _refresh.Height=38; _refresh.Text=LocalizationService.IsPersian?"به‌روزرسانی":"Refresh"; _refresh.Click+=(s,e)=>LoadData();
            ConfigureGrid(_workers); ConfigureGrid(_queues); ConfigureGrid(_history);
            _workerTitle.Dock=DockStyle.Top; _workerTitle.Height=32; _workerTitle.Font=new Font("Segoe UI Semibold",11F);
            _queueTitle.Dock=DockStyle.Top; _queueTitle.Height=32; _queueTitle.Font=new Font("Segoe UI Semibold",11F);
            var workerPanel=new Panel{Dock=DockStyle.Top,Height=210,Padding=new Padding(0,4,0,12)}; workerPanel.Controls.Add(_workers); workerPanel.Controls.Add(_workerTitle);
            var queuePanel=new Panel{Dock=DockStyle.Fill,Padding=new Padding(0,4,0,0)}; queuePanel.Controls.Add(_queues); queuePanel.Controls.Add(_queueTitle);
            _historyTitle.Dock=DockStyle.Top; _historyTitle.Height=32; _historyTitle.Font=new Font("Segoe UI Semibold",11F);
            var historyPanel=new Panel{Dock=DockStyle.Bottom,Height=205,Padding=new Padding(0,4,0,0)}; historyPanel.Controls.Add(_history); historyPanel.Controls.Add(_historyTitle);
            Controls.Add(queuePanel); Controls.Add(historyPanel); Controls.Add(workerPanel); Controls.Add(_refresh); Controls.Add(_title);
            RightToLeft=LocalizationService.IsPersian?RightToLeft.Yes:RightToLeft.No; RightToLeftLayout=LocalizationService.IsPersian;
            _timer.Interval=15000; _timer.Tick+=(s,e)=>LoadData(); _timer.Start(); Disposed+=(s,e)=>_timer.Dispose();
            LoadData();
        }
        private static void ConfigureGrid(DataGridView grid)
        { grid.Dock=DockStyle.Fill; grid.ReadOnly=true; grid.AllowUserToAddRows=false; grid.AllowUserToDeleteRows=false; grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill; grid.BackgroundColor=Color.White; grid.BorderStyle=BorderStyle.None; grid.RowHeadersVisible=false; grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; }
        private void LoadData()
        {
            try
            {
                _workers.DataSource=BackgroundWorkerMonitoringService.GetOverview();
                _queues.DataSource=BackgroundWorkerMonitoringService.GetQueueSummary();
                _history.DataSource=BackgroundWorkerMonitoringService.GetRecentHistory(24);
                _title.Text=LocalizationService.IsPersian?"مرکز عملیات و پایش سرویس‌ها":"Operations Center & Service Monitoring";
                _workerTitle.Text=LocalizationService.IsPersian?"وضعیت سرویس‌ها":"Worker Status";
                _queueTitle.Text=LocalizationService.IsPersian?"وضعیت صف‌ها":"Queue Status";
                _historyTitle.Text=LocalizationService.IsPersian?"تاریخچه ضربان ۲۴ ساعت اخیر":"Last 24 Hours — Heartbeat History";
                LocalizeHeaders(_workers); LocalizeHeaders(_queues); LocalizeHeaders(_history);
                foreach(DataGridViewRow row in _workers.Rows)
                { var status=Convert.ToString(row.Cells["RuntimeStatus"].Value); row.DefaultCellStyle.ForeColor=status=="Online"?Color.DarkGreen:(status=="Error"||status=="Offline"?Color.DarkRed:Color.DarkGoldenrod); }
            }
            catch(Exception ex) { MessageBox.Show((LocalizationService.IsPersian?"دریافت وضعیت سرویس‌ها ناموفق بود: ":"Unable to load service status: ")+ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning); }
        }
        private void LocalizeHeaders(DataGridView grid)
        { foreach(DataGridViewColumn c in grid.Columns) { string h=c.Name; if(LocalizationService.IsPersian) { if(h=="WorkerName")c.HeaderText="نام سرویس"; else if(h=="LastHeartbeatUtc")c.HeaderText="آخرین ضربان (UTC)"; else if(h=="RuntimeStatus")c.HeaderText="وضعیت"; else if(h=="LastStatus")c.HeaderText="وضعیت آخرین اجرا"; else if(h=="LastError")c.HeaderText="آخرین خطا"; else if(h=="SecondsSinceHeartbeat")c.HeaderText="ثانیه از آخرین فعالیت"; else if(h=="QueueName")c.HeaderText="صف"; else if(h=="Pending")c.HeaderText="در انتظار"; else if(h=="Completed")c.HeaderText="تکمیل‌شده"; else if(h=="Retried")c.HeaderText="ناموفق/تلاش مجدد"; else if(h=="HeartbeatUtc")c.HeaderText="زمان (UTC)"; else if(h=="Status")c.HeaderText="وضعیت"; else if(h=="ErrorMessage")c.HeaderText="شرح خطا"; } else c.HeaderText=h; } }
    }
}
