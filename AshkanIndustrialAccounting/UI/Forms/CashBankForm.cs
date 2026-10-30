using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;
using AshkanIndustrialAccounting.UI.Controls;
namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class CashBankForm : Form
    {
        private readonly DataGridView _grid=UiFactory.Grid();
        public CashBankForm()
        {
            BackColor=Color.FromArgb(242,246,251);
            var bar=new Panel{Dock=DockStyle.Top,Height=62,BackColor=Color.White,Padding=new Padding(12)};
            var title=new Label{Text=LocalizationService.IsPersian?"صندوق و بانک":"Cash & Bank",Dock=DockStyle.Fill,
                Font=new Font("Segoe UI Semibold",14F),ForeColor=Color.FromArgb(22,43,70),
                TextAlign=LocalizationService.IsPersian?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft};
            var refresh=UiFactory.Primary(LocalizationService.IsPersian?"↻ بروزرسانی":"↻ Refresh");
            refresh.Dock=LocalizationService.IsPersian?DockStyle.Left:DockStyle.Right; refresh.Click+=(s,e)=>LoadData();
            var receipt=UiFactory.Primary(LocalizationService.IsPersian?"ثبت دریافت":"New receipt");
            var payment=UiFactory.Primary(LocalizationService.IsPersian?"ثبت پرداخت":"New payment");
            receipt.Dock=DockStyle.Right; payment.Dock=DockStyle.Right;
            receipt.Click+=(s,e)=>Post("Receipt");payment.Click+=(s,e)=>Post("Payment");
            var history=UiFactory.Primary(LocalizationService.IsPersian?"گردش حساب":"Account statement");
            history.Dock=DockStyle.Right;history.Click+=(s,e)=>ShowStatement();
            var transfer=UiFactory.Primary(LocalizationService.IsPersian?"انتقال وجه":"Transfer funds");
            transfer.Dock=DockStyle.Right; transfer.Click+=(s,e)=>TransferFunds();
            var reconciliation=UiFactory.Primary(LocalizationService.IsPersian?"مغایرت بانکی":"Bank reconciliation");
            reconciliation.Dock=DockStyle.Right;
            reconciliation.Click+=(s,e)=>{using(var dialog=new BankReconciliationForm())dialog.ShowDialog(this);};
            bar.Controls.Add(reconciliation);
            bar.Controls.Add(transfer);bar.Controls.Add(history);bar.Controls.Add(title);bar.Controls.Add(refresh);bar.Controls.Add(payment);bar.Controls.Add(receipt);
            Controls.Add(_grid);Controls.Add(bar);Shown+=(s,e)=>LoadData();
        }
        private void Post(string direction)
        {
            try
            {
                AuthorizationService.RequireActiveSession("cashbank");
                using(var dialog=new Form{Text=direction,Width=480,Height=480,StartPosition=FormStartPosition.CenterParent})
                {
                    var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=11,Padding=new Padding(14),AutoScroll=true};
                    var account=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
                    using(var cn=Db.Open())using(var da=new SqlDataAdapter("SELECT Id,Code+' - '+Name AS Name FROM dbo.CashBankAccounts WHERE IsActive=1 AND CurrencyCode='IRR' ORDER BY Name",cn))
                    {var dt=new DataTable();da.Fill(dt);account.DataSource=dt;}
                    var offset=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
                    using(var cn=Db.Open())using(var da=new SqlDataAdapter("SELECT Id,Code+' - '+NameEn AS Name FROM dbo.Accounts WHERE IsActive=1 ORDER BY Code",cn))
                    {var dt=new DataTable();da.Fill(dt);offset.DataSource=dt;}
                    var partner=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
                    using(var cn=Db.Open())using(var da=new SqlDataAdapter("SELECT Id,Name FROM dbo.BusinessPartners ORDER BY Name",cn))
                    {var dt=new DataTable();da.Fill(dt);var empty=dt.NewRow();empty["Id"]=0;empty["Name"]="—";dt.Rows.InsertAt(empty,0);partner.DataSource=dt;}
                    var amount=new NumericUpDown{Dock=DockStyle.Top,DecimalPlaces=2,Maximum=1000000000000m,Minimum=0.01m,Value=1m};
                    var reference=new TextBox{Dock=DockStyle.Top,MaxLength=80};
                    var description=new TextBox{Dock=DockStyle.Top,MaxLength=500};
                    var submit=new Button{Text=LocalizationService.IsPersian?"ثبت نهایی":"Post transaction",Dock=DockStyle.Top,DialogResult=DialogResult.OK};
                    panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"حساب":"Account"});panel.Controls.Add(account);
                    panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"سرفصل مقابل":"Offset ledger"});panel.Controls.Add(offset);
                    panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"طرف حساب (اختیاری)":"Business partner (optional)"});panel.Controls.Add(partner);
                    panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"مبلغ (ریال)":"Amount (IRR)"});panel.Controls.Add(amount);
                    panel.Controls.Add(reference);panel.Controls.Add(description);panel.Controls.Add(submit);
                    dialog.Controls.Add(panel);dialog.AcceptButton=submit;
                    if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                    if(account.SelectedValue==null)throw new InvalidOperationException("Select an account.");
                    CashBankTransactionService.Post(Convert.ToInt32(account.SelectedValue),direction,amount.Value,reference.Text,description.Text,
                        Convert.ToInt32(offset.SelectedValue),Convert.ToInt32(partner.SelectedValue)==0?(int?)null:Convert.ToInt32(partner.SelectedValue));
                    LoadData();
                }
            }
            catch(Exception ex){MessageBox.Show(ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        private void TransferFunds()
        {
            try
            {
                AuthorizationService.RequireActiveSession("cashbank");
                using(var dialog=new Form{Text=LocalizationService.IsPersian?"انتقال بین حساب‌ها":"Transfer between accounts",
                    Width=520,Height=420,StartPosition=FormStartPosition.CenterParent})
                {
                    var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true,Padding=new Padding(14)};
                    var source=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
                    var destination=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
                    using(var cn=Db.Open())using(var da=new SqlDataAdapter(
                        "SELECT Id,Code+' - '+Name AS Name FROM dbo.CashBankAccounts WHERE IsActive=1 AND CurrencyCode='IRR' ORDER BY Name",cn))
                    {
                        var accounts=new DataTable();da.Fill(accounts);
                        source.DataSource=accounts.Copy();destination.DataSource=accounts.Copy();
                    }
                    var amount=new NumericUpDown{Dock=DockStyle.Top,DecimalPlaces=2,Maximum=1000000000000m,Minimum=0.01m,Value=1m};
                    var key=new TextBox{Dock=DockStyle.Top,MaxLength=80,Text=Guid.NewGuid().ToString("N")};
                    var note=new TextBox{Dock=DockStyle.Top,MaxLength=500};
                    var submit=new Button{Text=LocalizationService.IsPersian?"ثبت انتقال":"Post transfer",Dock=DockStyle.Top,DialogResult=DialogResult.OK};
                    panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"از حساب":"From account"});panel.Controls.Add(source);
                    panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"به حساب":"To account"});panel.Controls.Add(destination);
                    panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"مبلغ (ریال)":"Amount (IRR)"});panel.Controls.Add(amount);
                    panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"شناسه یکتای درخواست":"Unique request key"});panel.Controls.Add(key);
                    panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"شرح":"Description"});panel.Controls.Add(note);
                    panel.Controls.Add(submit);dialog.Controls.Add(panel);dialog.AcceptButton=submit;
                    if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                    if(source.SelectedValue==null || destination.SelectedValue==null)
                        throw new InvalidOperationException("Select both accounts.");
                    CashBankTransferService.Transfer(Convert.ToInt32(source.SelectedValue),
                        Convert.ToInt32(destination.SelectedValue),amount.Value,key.Text,note.Text);
                    LoadData();
                }
            }
            catch(Exception ex){MessageBox.Show(ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        private void ShowStatement()
        {
            try
            {
                AuthorizationService.RequireActiveSession("cashbank");
                using(var dialog=new Form{Text=LocalizationService.IsPersian?"گردش صندوق و بانک":"Cash & bank statement",
                    Width=1000,Height=600,StartPosition=FormStartPosition.CenterParent})
                {
                    var grid=UiFactory.Grid();grid.Dock=DockStyle.Fill;
                    var filters=new FlowLayoutPanel{Dock=DockStyle.Top,Height=55,Padding=new Padding(8)};
                    var accounts=new ComboBox{Width=190,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
                    using(var cn=Db.Open())using(var da=new SqlDataAdapter(
                        "SELECT Id,Code+' - '+Name AS Name FROM dbo.CashBankAccounts WHERE IsActive=1 ORDER BY Name",cn))
                    {var dt=new DataTable();da.Fill(dt);accounts.DataSource=dt;}
                    var from=new DateTimePicker{Width=140,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddMonths(-1)};
                    var until=new DateTimePicker{Width=140,Format=DateTimePickerFormat.Short,Value=DateTime.Today};
                    var run=new Button{Text=LocalizationService.IsPersian?"نمایش گردش":"Load statement",AutoSize=true};
                    run.Click+=(sender,args)=>
                    {
                        try
                        {
                            if(accounts.SelectedValue==null)return;
                            if(from.Value.Date>until.Value.Date)throw new ArgumentException("Invalid date range.");
                            using(var cn=Db.Open())using(var cmd=new SqlCommand(@"
WITH Events AS (
 SELECT p.PaymentDate AS EventDate,p.Id AS EventId,
        p.PaymentNo AS Reference, p.Direction AS EventType,
        CASE WHEN p.Direction=N'Receipt' THEN p.Amount ELSE -p.Amount END AS Delta
 FROM dbo.Payments p WHERE p.CashBankAccountId=@account
 UNION ALL
 SELECT t.CreatedAt,t.Id,N'TR-'+CONVERT(nvarchar(20),t.Id),N'Transfer out',-t.Amount
 FROM dbo.CashBankTransfers t WHERE t.SourceAccountId=@account
 UNION ALL
 SELECT t.CreatedAt,t.Id,N'TR-'+CONVERT(nvarchar(20),t.Id),N'Transfer in',t.Amount
 FROM dbo.CashBankTransfers t WHERE t.DestinationAccountId=@account
), Running AS (
 SELECT EventDate,EventId,Reference,EventType,Delta,
        SUM(Delta) OVER(ORDER BY EventDate,Reference,EventType,EventId
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS CumulativeDelta
 FROM Events
)
SELECT EventDate,Reference,EventType,Delta,
       (SELECT Balance FROM dbo.CashBankAccounts WHERE Id=@account)
       - (SELECT COALESCE(SUM(Delta),0) FROM Events) + CumulativeDelta AS RunningBalance
FROM Running WHERE EventDate>=@from AND EventDate<@until
ORDER BY EventDate,Reference,EventType,EventId",cn))
                            {
                                cmd.Parameters.Add("@account",SqlDbType.Int).Value=Convert.ToInt32(accounts.SelectedValue);
                                cmd.Parameters.Add("@from",SqlDbType.DateTime2).Value=from.Value.Date;
                                cmd.Parameters.Add("@until",SqlDbType.DateTime2).Value=until.Value.Date.AddDays(1);
                                using(var da=new SqlDataAdapter(cmd)){var dt=new DataTable();da.Fill(dt);grid.DataSource=dt;}
                            }
                        }
                        catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                    };
                    filters.Controls.Add(accounts);filters.Controls.Add(from);filters.Controls.Add(until);filters.Controls.Add(run);
                    dialog.Controls.Add(grid);dialog.Controls.Add(filters);
                    run.PerformClick();
                    dialog.ShowDialog(this);
                }
            }
            catch(Exception ex){MessageBox.Show(ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        private void LoadData()
        {
            try{using(var cn=Db.Open())using(var da=new SqlDataAdapter(@"SELECT Code,Name,AccountType AS [Type],CurrencyCode AS [Currency],Balance,IsActive AS [Active]
FROM CashBankAccounts ORDER BY AccountType,Name",cn)){var dt=new DataTable();da.Fill(dt);_grid.DataSource=dt;}}
            catch(Exception ex){MessageBox.Show(ex.Message,LocalizationService.IsPersian?"خطای دیتابیس":"Database",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
    }
}