using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class BusinessPartnersForm : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _search = new TextBox();

        public BusinessPartnersForm()
        {
            BackColor = Color.FromArgb(242, 246, 251);
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.White, Padding = new Padding(14) };
            _search.Width = 280; _search.Dock = LocalizationService.IsPersian ? DockStyle.Right : DockStyle.Left;
            var add = new Button { Text = LocalizationService.IsPersian ? "+ طرف حساب جدید" : "+ New partner", Width = 150, Dock = LocalizationService.IsPersian ? DockStyle.Left : DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(25, 99, 180), ForeColor = Color.White };
            add.FlatAppearance.BorderSize = 0; add.Click += (s,e) => AddPartner();
            _search.TextChanged += (s,e) => LoadData();
            toolbar.Controls.Add(_search); toolbar.Controls.Add(add);

            _grid.Dock = DockStyle.Fill; _grid.BackgroundColor = Color.White; _grid.BorderStyle = BorderStyle.None;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.ReadOnly = true; _grid.RowHeadersVisible = false;
            _grid.AllowUserToAddRows = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.EnableHeadersVisualStyles = false; _grid.ColumnHeadersHeight = 42; _grid.RowTemplate.Height = 38;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235,241,248);
            Controls.Add(_grid); Controls.Add(toolbar); Shown += (s,e) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                using(var cn=Db.Open())
                using(var da=new SqlDataAdapter(@"SELECT Code AS [Code], Name AS [Name], PartnerType AS [Type], Phone, Email, TaxId AS [Tax ID], CreditLimit AS [Credit Limit], IsActive AS [Active]
FROM BusinessPartners WHERE (@q='' OR Code LIKE @like OR Name LIKE @like) ORDER BY Name",cn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@q", _search.Text.Trim());
                    da.SelectCommand.Parameters.AddWithValue("@like", "%" + _search.Text.Trim() + "%");
                    var dt=new DataTable(); da.Fill(dt); _grid.DataSource=dt;
                }
            } catch(Exception ex) { MessageBox.Show(ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void AddPartner()
        {
            using(var f=new Form { Text=LocalizationService.IsPersian?"طرف حساب جدید":"New business partner", Width=460, Height=430, StartPosition=FormStartPosition.CenterParent, Font=new Font("Segoe UI",10F) })
            {
                var panel=new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(22), ColumnCount=2, RowCount=7 };
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));
                var code = new TextBox();
                var name = new TextBox();
                var phone = new TextBox();
                var email = new TextBox();
                var tax = new TextBox();
                var type=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList }; type.Items.AddRange(new object[]{"Customer","Supplier","Both"}); type.SelectedIndex=0;
                AddRow(panel,0,LocalizationService.IsPersian?"کد":"Code",code); AddRow(panel,1,LocalizationService.IsPersian?"نام":"Name",name);
                AddRow(panel,2,LocalizationService.IsPersian?"نوع":"Type",type); AddRow(panel,3,LocalizationService.IsPersian?"تلفن":"Phone",phone);
                AddRow(panel,4,"Email",email); AddRow(panel,5,LocalizationService.IsPersian?"شناسه مالیاتی":"Tax ID",tax);
                var save=new Button { Text=LocalizationService.IsPersian?"ذخیره":"Save",Dock=DockStyle.Fill,Height=40,BackColor=Color.FromArgb(25,99,180),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
                panel.Controls.Add(save,1,6); save.Click+=(s,e)=>{
                    if(string.IsNullOrWhiteSpace(code.Text)||string.IsNullOrWhiteSpace(name.Text)){MessageBox.Show(LocalizationService.IsPersian?"کد و نام الزامی است.":"Code and name are required.");return;}
                    try { using(var cn=Db.Open()) using(var cmd=new SqlCommand("INSERT INTO BusinessPartners(Code,Name,PartnerType,Phone,Email,TaxId) VALUES(@c,@n,@t,@p,@e,@x)",cn)){cmd.Parameters.AddWithValue("@c",code.Text.Trim());cmd.Parameters.AddWithValue("@n",name.Text.Trim());cmd.Parameters.AddWithValue("@t",type.Text);cmd.Parameters.AddWithValue("@p",phone.Text.Trim());cmd.Parameters.AddWithValue("@e",email.Text.Trim());cmd.Parameters.AddWithValue("@x",tax.Text.Trim());cmd.ExecuteNonQuery();} f.DialogResult=DialogResult.OK; f.Close(); } catch(Exception ex){MessageBox.Show(ex.Message);}
                };
                f.Controls.Add(panel); if(f.ShowDialog(this)==DialogResult.OK) LoadData();
            }
        }
        private static void AddRow(TableLayoutPanel p,int row,string label,Control c){p.RowStyles.Add(new RowStyle(SizeType.Absolute,48));p.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,row);c.Dock=DockStyle.Fill;p.Controls.Add(c,1,row);}
    }
}