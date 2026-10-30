using System.Drawing;
using System.Windows.Forms;
namespace AshkanIndustrialAccounting.UI.Controls
{
    public static class UiFactory
    {
        public static DataGridView Grid()
        {
            var g=new DataGridView { Dock=DockStyle.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,
                AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,ReadOnly=true,AllowUserToAddRows=false,
                AllowUserToDeleteRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,
                MultiSelect=false,EnableHeadersVisualStyles=false };
            g.ColumnHeadersHeight=42; g.RowTemplate.Height=38;
            g.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(235,241,248);
            g.ColumnHeadersDefaultCellStyle.ForeColor=Color.FromArgb(22,43,70);
            g.DefaultCellStyle.SelectionBackColor=Color.FromArgb(220,235,251);
            g.DefaultCellStyle.SelectionForeColor=Color.FromArgb(22,43,70);
            return g;
        }
        public static Button Primary(string text)
        {
            var b=new Button{Text=text,Height=40,Width=155,FlatStyle=FlatStyle.Flat,
                BackColor=Color.FromArgb(25,99,180),ForeColor=Color.White,Cursor=Cursors.Hand};
            b.FlatAppearance.BorderSize=0; return b;
        }
    }
}