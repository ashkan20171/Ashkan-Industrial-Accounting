using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Core.Models;
using AshkanIndustrialAccounting.Services;
using AshkanIndustrialAccounting.UI.Controls;
namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class JournalEntryForm : Form
    {
        private readonly DataGridView _grid=new DataGridView();
        private readonly TextBox _no=new TextBox();
        private readonly PlaceholderTextBox _desc=new PlaceholderTextBox();
        public JournalEntryForm()
        {
            BackColor=Color.FromArgb(245,248,252);
            var title=new Label{Text=LocalizationService.T("journal"),Dock=DockStyle.Top,Height=58,Font=new Font("Segoe UI Semibold",22)};
            var header=new FlowLayoutPanel{Dock=DockStyle.Top,Height=58,Padding=new Padding(0,8,0,0)};
            _no.Width=160;_no.Text="JE-"+DateTime.Now.ToString("yyyyMMdd-HHmm");_desc.Width=420;_desc.Placeholder=LocalizationService.IsPersian?"شرح سند":"Description";
            header.Controls.Add(_no);header.Controls.Add(_desc);
            _grid.Dock=DockStyle.Fill;_grid.BackgroundColor=Color.White;_grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Columns.Add("AccountId","Account Id");_grid.Columns.Add("AccountCode","Account Code");_grid.Columns.Add("Description","Description");_grid.Columns.Add("Debit","Debit");_grid.Columns.Add("Credit","Credit");_grid.Columns.Add("CostCenterId","Cost Center");
            var footer=new Panel{Dock=DockStyle.Bottom,Height=62};
            var post=new Button{Text=LocalizationService.IsPersian?"ثبت قطعی سند":"Post Journal",Dock=DockStyle.Right,Width=160,BackColor=Color.FromArgb(25,99,180),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};post.FlatAppearance.BorderSize=0;post.Click+=(s,e)=>Post();
            footer.Controls.Add(post);Controls.Add(_grid);Controls.Add(footer);Controls.Add(header);Controls.Add(title);
        }
        private void Post()
        {
            try{
                var e=new JournalEntry{EntryNo=_no.Text,EntryDate=DateTime.Now,Description=_desc.Text};
                foreach(DataGridViewRow r in _grid.Rows) if(!r.IsNewRow)
                    e.Lines.Add(new JournalLine{AccountId=Convert.ToInt32(r.Cells[0].Value),AccountCode=Convert.ToString(r.Cells[1].Value),Description=Convert.ToString(r.Cells[2].Value),Debit=ToDecimal(r.Cells[3].Value),Credit=ToDecimal(r.Cells[4].Value),CostCenterId=ToNullableInt(r.Cells[5].Value)});
                var id=new AccountingService().Post(e);MessageBox.Show("Journal posted. ID: "+id,"Success");
            }catch(Exception ex){MessageBox.Show(ex.Message,"Validation",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        private static decimal ToDecimal(object v){decimal x;return decimal.TryParse(Convert.ToString(v),out x)?x:0;}
        private static int? ToNullableInt(object v){int x;return int.TryParse(Convert.ToString(v),out x)?(int?)x:null;}
    }
}
