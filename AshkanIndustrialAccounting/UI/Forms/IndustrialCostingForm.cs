using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;
namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class IndustrialCostingForm : Form
    {
        public IndustrialCostingForm()
        {
            BackColor=Color.FromArgb(245,248,252);
            var title=new Label{Text=LocalizationService.T("costing"),Dock=DockStyle.Top,Height=58,Font=new Font("Segoe UI Semibold",22)};
            var tabs=new TabControl{Dock=DockStyle.Fill};
            string[] en={"Cost Centers","Work Orders","BOM Costing","Machine Hours","Labor","Overhead Allocation","Variance Analysis"};
            string[] fa={"مراکز هزینه","دستورکارها","بهای BOM","ساعت ماشین","دستمزد","تسهیم سربار","تحلیل انحراف"};
            for(int i=0;i<en.Length;i++){var p=new TabPage(LocalizationService.IsPersian?fa[i]:en[i]);var l=new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Segoe UI",13),ForeColor=Color.DimGray,Text=(LocalizationService.IsPersian?"ماژول آماده توسعه و اتصال به داده واقعی: ":"Production-ready module boundary: ")+(LocalizationService.IsPersian?fa[i]:en[i])};p.Controls.Add(l);tabs.TabPages.Add(p);}
            Controls.Add(tabs);Controls.Add(title);
        }
    }
}
