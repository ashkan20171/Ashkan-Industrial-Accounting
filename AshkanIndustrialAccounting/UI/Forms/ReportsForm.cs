using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;
namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class ReportsForm : Form
    {
        public ReportsForm()
        {
            BackColor=Color.FromArgb(245,248,252);
            var title=new Label{Text=LocalizationService.T("reports"),Dock=DockStyle.Top,Height=58,Font=new Font("Segoe UI Semibold",22)};
            var list=new ListBox{Dock=DockStyle.Left,Width=310,Font=new Font("Segoe UI",11),BorderStyle=BorderStyle.None};
            list.Items.AddRange((LocalizationService.IsPersian?new object[]{"تراز آزمایشی","دفتر کل","صورت سود و زیان","ترازنامه","جریان وجوه نقد","گزارش مراکز هزینه","بهای تمام‌شده تولید","تحلیل انحراف بودجه"}:new object[]{"Trial Balance","General Ledger","Profit & Loss","Balance Sheet","Cash Flow Statement","Cost Center Report","Manufacturing Cost Statement","Budget Variance Analysis"}));
            var preview=new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Segoe UI",13),ForeColor=Color.DimGray,Text=LocalizationService.IsPersian?"یک گزارش را برای پیش‌نمایش انتخاب کنید.":"Select a report to preview."};
            var valuation = new Button { Dock=DockStyle.Bottom, Height=48,
                Text=LocalizationService.IsPersian ? "گزارش ارزش و مغایرت موجودی" : "Inventory valuation & reconciliation",
                BackColor=Color.FromArgb(35,99,175),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            valuation.Click += (sender,args) => { using(var dialog=new InventoryValuationReportForm()) { dialog.Size=new Size(1050,650); dialog.StartPosition=FormStartPosition.CenterParent; dialog.ShowDialog(this); } };
            var grossProfit = new Button { Dock=DockStyle.Bottom, Height=48,
                Text=LocalizationService.IsPersian ? "گزارش سود ناخالص فروش" : "Sales gross profit",
                BackColor=Color.FromArgb(22,124,105),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            grossProfit.Click += (sender,args) => { using(var dialog=new GrossProfitReportForm()) {
                dialog.Size=new Size(1100,650); dialog.StartPosition=FormStartPosition.CenterParent;
                dialog.ShowDialog(this); } };
            var products = new Button { Dock=DockStyle.Bottom,Height=48,
                Text=LocalizationService.IsPersian ? "تحلیل سودآوری محصولات" : "Product profitability",
                BackColor=Color.FromArgb(65,83,155),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            products.Click += (sender,args) => { using(var dialog=new ProductProfitabilityForm()) {
                dialog.Size=new Size(1100,650);dialog.StartPosition=FormStartPosition.CenterParent;
                dialog.ShowDialog(this); } };
            Controls.Add(products);
            var trends = new Button { Dock=DockStyle.Bottom,Height=48,
                Text=LocalizationService.IsPersian ? "روند ماهانه فروش و سود" : "Monthly financial trends",
                BackColor=Color.FromArgb(20,119,105),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            trends.Click += (sender,args) => { using(var dialog=new FinancialTrendsForm()) {
                dialog.Size=new Size(1100,700);dialog.StartPosition=FormStartPosition.CenterParent;
                dialog.ShowDialog(this); } };
            Controls.Add(trends);
            var audit=new Button { Dock=DockStyle.Bottom,Height=48,
                Text=LocalizationService.IsPersian ? "ممیزی یکپارچگی مالی و انبار" : "Financial integrity audit",
                BackColor=Color.FromArgb(156,82,53),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            audit.Click+=(sender,args)=>{using(var dialog=new FinancialIntegrityAuditForm()) {
                dialog.Size=new Size(1100,680);dialog.StartPosition=FormStartPosition.CenterParent;
                dialog.ShowDialog(this);}};
            Controls.Add(audit);
            if (AuthorizationService.CanAccess("auditlog"))
            {
                var history = new Button { Dock=DockStyle.Bottom,Height=48,
                    Text=LocalizationService.IsPersian ? "تاریخچه فعالیت کاربران" : "User activity history",
                    BackColor=Color.FromArgb(58,77,115),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
                history.Click+=(sender,args)=>{ using(var dialog=new AuditHistoryForm()) {
                    dialog.Size=new Size(1150,700);dialog.StartPosition=FormStartPosition.CenterParent;
                    dialog.ShowDialog(this); } };
                Controls.Add(history);
            }
            Controls.Add(preview);Controls.Add(list);Controls.Add(grossProfit);Controls.Add(valuation);Controls.Add(title);
        }
    }
}
