using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;
using AshkanIndustrialAccounting.UI.Controls;
namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class SalesForm : Form
    {
        private readonly DataGridView _grid=UiFactory.Grid();
        private readonly Button _create = UiFactory.Primary(LocalizationService.IsPersian ? "فاکتور جدید" : "New invoice");
        private readonly Button _return = UiFactory.Primary(LocalizationService.IsPersian ? "برگشت کامل از فروش" : "Return invoice");
        private readonly Button _post = UiFactory.Primary(LocalizationService.IsPersian ? "ثبت قطعی و خروج انبار" : "Post & Issue Stock");
        private readonly GridSearchToolbar _search;
        public SalesForm()
        {
            BackColor=Color.FromArgb(242,246,251);
            _search = new GridSearchToolbar(_grid);
            var bar=new Panel{Dock=DockStyle.Top,Height=62,BackColor=Color.White,Padding=new Padding(12)};
            var title=new Label{Text=LocalizationService.IsPersian?"فروش و فاکتورها":"Sales & Invoices",Dock=DockStyle.Fill,
                Font=new Font("Segoe UI Semibold",14F),ForeColor=Color.FromArgb(22,43,70),
                TextAlign=LocalizationService.IsPersian?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft};
            var refresh=UiFactory.Primary(LocalizationService.IsPersian?"↻ بروزرسانی":"↻ Refresh");
            refresh.Dock=LocalizationService.IsPersian?DockStyle.Left:DockStyle.Right; refresh.Click+=(s,e)=>LoadData();
            _post.Dock = LocalizationService.IsPersian ? DockStyle.Left : DockStyle.Right;
            _post.Click += (s,e) => PostSelected();
            _create.Dock = LocalizationService.IsPersian ? DockStyle.Left : DockStyle.Right;
            _create.Click += (s,e) => CreateInvoice();
            _return.Dock = LocalizationService.IsPersian ? DockStyle.Left : DockStyle.Right;
            _return.Click += (s,e) => ReturnSelected();
            bar.Controls.Add(title);bar.Controls.Add(_return);bar.Controls.Add(_create);bar.Controls.Add(_post);bar.Controls.Add(refresh);Controls.Add(_grid);Controls.Add(_search);Controls.Add(bar);Shown+=(s,e)=>LoadData();
        }
        private void CreateInvoice()
        {
            try
            {
                var customers=new DataTable(); var products=new DataTable();
                using(var cn=Db.Open()) {
                    using(var da=new SqlDataAdapter("SELECT Id,Name FROM dbo.BusinessPartners WHERE IsActive=1 AND PartnerType IN ('Customer','Both') ORDER BY Name",cn))da.Fill(customers);
                    using(var da=new SqlDataAdapter("SELECT Id,Name,SalePrice FROM dbo.Products WHERE IsActive=1 ORDER BY Name",cn))da.Fill(products);
                }
                if(customers.Rows.Count==0||products.Rows.Count==0)throw new InvalidOperationException("Add a customer and product first.");
                using(var f=new Form{Text=LocalizationService.IsPersian?"صدور پیش‌نویس فاکتور":"Create draft invoice",Width=640,Height=510,StartPosition=FormStartPosition.CenterParent,BackColor=Color.White})
                {
                    var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=2,RowCount=6};
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,150));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
                    var number=new TextBox{Dock=DockStyle.Fill,Text="INV-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")};
                    var customer=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DataSource=customers,DisplayMember="Name",ValueMember="Id"};
                    var product=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DataSource=products,DisplayMember="Name",ValueMember="Id"};
                    var qty=new NumericUpDown{Dock=DockStyle.Fill,DecimalPlaces=3,Minimum=0.001M,Maximum=1000000,Value=1};
                    var add=UiFactory.Primary(LocalizationService.IsPersian?"افزودن ردیف":"Add line");
                    var lines=new List<InvoiceItem>();var preview=new ListBox{Dock=DockStyle.Fill};
                    Action<string,Control,int> row=(label,control,index)=>{layout.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,index);layout.Controls.Add(control,1,index);};
                    row("Invoice No",number,0);row("Customer",customer,1);row("Product",product,2);row("Quantity",qty,3);
                    var panel=new Panel{Dock=DockStyle.Fill};add.Dock=DockStyle.Right;panel.Controls.Add(add);layout.Controls.Add(panel,1,4);
                    add.Click+=(sender,args)=>{var selected=products.Select("Id="+Convert.ToInt32(product.SelectedValue));if(selected.Length!=1)return;var item=new InvoiceItem{ProductId=Convert.ToInt32(product.SelectedValue),Quantity=qty.Value,UnitPrice=Convert.ToDecimal(selected[0]["SalePrice"])};lines.Add(item);preview.Items.Add(product.Text+" × "+item.Quantity+" = "+(item.Quantity*item.UnitPrice).ToString("N2"));};
                    var bottom=new Panel{Dock=DockStyle.Bottom,Height=54};var save=UiFactory.Primary(LocalizationService.IsPersian?"ذخیره پیش‌نویس":"Save draft");save.Dock=DockStyle.Right;bottom.Controls.Add(save);
                    save.Click+=(sender,args)=>{try{InvoiceDraftService.Create(number.Text,Convert.ToInt32(customer.SelectedValue),lines);f.DialogResult=DialogResult.OK;f.Close();}catch(Exception ex){MessageBox.Show(ex.Message,"Invoice",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
                    var content=new Panel{Dock=DockStyle.Fill};content.Controls.Add(preview);content.Controls.Add(layout);layout.Dock=DockStyle.Top;layout.Height=235;f.Controls.Add(content);f.Controls.Add(bottom);
                    if(f.ShowDialog(this)==DialogResult.OK)LoadData();
                }
            }catch(Exception ex){MessageBox.Show(ex.Message,"Invoice",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        private void ReturnSelected()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.Cells["Id"] == null) return;
            int id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            string message = LocalizationService.IsPersian ? "کل فاکتور برگشت داده شود و موجودی انبار افزایش یابد؟" : "Return the entire invoice and restore stock?";
            if (MessageBox.Show(message, "Confirm return", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { SalesReturnService.ReturnPosted(id, AuthorizationService.CurrentUserName); LoadData(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Return failed", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private void PostSelected()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.Cells["Id"] == null) return;
            int id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            if (MessageBox.Show(LocalizationService.IsPersian ? "فاکتور قطعی و موجودی انبار کسر شود؟" : "Post invoice and deduct inventory?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { SalesPostingService.Post(id, AuthorizationService.CurrentUserName); LoadData(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Posting failed", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private void LoadData()
        {
            try{using(var cn=Db.Open())using(var da=new SqlDataAdapter(@"SELECT s.Id, s.InvoiceNo AS [Invoice],s.InvoiceDate AS [Date],p.Name AS [Customer],s.CurrencyCode AS [Currency],
s.Subtotal,s.Discount,s.Tax,s.Total,s.Status FROM SalesInvoices s
INNER JOIN BusinessPartners p ON p.Id=s.PartnerId ORDER BY s.InvoiceDate DESC",cn)){var dt=new DataTable();da.Fill(dt);_search.Bind(dt); if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;}}
            catch(Exception ex){MessageBox.Show(ex.Message,LocalizationService.IsPersian?"خطای دیتابیس":"Database",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
    }
}