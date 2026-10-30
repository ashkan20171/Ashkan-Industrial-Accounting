using System.Drawing;
using System.Windows.Forms;
namespace AshkanIndustrialAccounting.UI.Controls
{
    public sealed class MetricCard : Panel
    {
        private readonly Label _title = new Label();
        private readonly Label _value = new Label();
        public MetricCard()
        {
            Width=230; Height=105; BackColor=Color.White; Padding=new Padding(16); Margin=new Padding(10);
            _title.Dock=DockStyle.Top; _title.Height=30; _title.ForeColor=Color.FromArgb(91,103,118); _title.Font=new Font("Segoe UI",10F);
            _value.Dock=DockStyle.Fill; _value.Font=new Font("Segoe UI Semibold",20F); _value.ForeColor=Color.FromArgb(18,38,63);
            Controls.Add(_value); Controls.Add(_title);
        }
        public void SetMetric(string title,string value){_title.Text=title;_value.Text=value;}
    }
}
