using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;
namespace AshkanIndustrialAccounting.UI.Forms
{
 public sealed class UserManagementForm : Form
 {
  private readonly DataGridView users=new DataGridView(), history=new DataGridView();
  private readonly TextBox username=new TextBox(), display=new TextBox(), password=new TextBox(), email=new TextBox();
  private readonly ComboBox role=new ComboBox(); private readonly Label status=new Label();
  public UserManagementForm()
  {
   Dock=DockStyle.Fill; BackColor=Color.FromArgb(242,246,251); Font=new Font("Segoe UI",9.5F);
   var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(5)}; root.RowStyles.Add(new RowStyle(SizeType.Absolute,205)); root.RowStyles.Add(new RowStyle(SizeType.Percent,48)); root.RowStyles.Add(new RowStyle(SizeType.Percent,52)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,28));
   var editor=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=3,BackColor=Color.White,Padding=new Padding(10)}; for(int i=0;i<4;i++) editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
   Field(editor,"نام کاربری / Username",username,0,0); Field(editor,"نام نمایشی / Display name",display,1,0); Field(editor,"رمز عبور / Password",password,2,0); password.UseSystemPasswordChar=true; role.DropDownStyle=ComboBoxStyle.DropDownList; role.Items.AddRange(new object[]{"Accountant","Manager","Administrator"}); role.SelectedIndex=0; Field(editor,"نقش / Role",role,3,0);
   Field(editor,"ایمیل کاربر انتخاب‌شده / User email",email,0,1);
   AddButton(editor,"ایجاد کاربر / Create",0,2,Color.FromArgb(25,99,180),CreateUser); AddButton(editor,"ذخیره ایمیل / Save email",1,2,Color.FromArgb(20,125,130),SaveEmail); AddButton(editor,"فعال/غیرفعال‌سازی / Toggle",2,2,Color.FromArgb(180,65,65),ToggleUser); AddButton(editor,"بروزرسانی / Refresh",3,2,Color.FromArgb(76,92,110),RefreshAll);
   ConfigureGrid(users); ConfigureGrid(history); users.SelectionChanged+=(s,e)=>LoadSelectedEmail();
   var usersHost=Host("کاربران / Users",users); var historyHost=Host("تاریخچه ارسال ایمیل / Email delivery history — برای تلاش مجدد، یک پیام DeadLetter را انتخاب کنید",history);
   var footer=new Panel{Dock=DockStyle.Fill}; status.Dock=DockStyle.Fill; status.TextAlign=ContentAlignment.MiddleLeft; status.ForeColor=Color.DimGray; footer.Controls.Add(status);
   root.Controls.Add(editor,0,0); root.Controls.Add(usersHost,0,1); root.Controls.Add(historyHost,0,2); root.Controls.Add(footer,0,3); Controls.Add(root);
   history.CellDoubleClick+=(s,e)=>RetrySelectedEmail(); Load+=(s,e)=>RefreshAll();
  }
  private static Panel Host(string title,Control grid){var p=new Panel{Dock=DockStyle.Fill,Padding=new Padding(2)};var l=new Label{Text=title,Dock=DockStyle.Top,Height=26,ForeColor=Color.FromArgb(55,72,94)};grid.Dock=DockStyle.Fill;p.Controls.Add(grid);p.Controls.Add(l);return p;}
  private static void ConfigureGrid(DataGridView g){g.ReadOnly=true;g.AllowUserToAddRows=false;g.AllowUserToDeleteRows=false;g.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;g.SelectionMode=DataGridViewSelectionMode.FullRowSelect;g.MultiSelect=false;g.BackgroundColor=Color.White;g.RowHeadersVisible=false;g.Dock=DockStyle.Fill;}
  private static void Field(TableLayoutPanel p,string title,Control c,int x,int y){var h=new Panel{Dock=DockStyle.Fill,Padding=new Padding(4)};var l=new Label{Text=title,Dock=DockStyle.Top,Height=25,ForeColor=Color.FromArgb(55,72,94)};c.Dock=DockStyle.Top;c.Height=28;h.Controls.Add(c);h.Controls.Add(l);p.Controls.Add(h,x,y);}
  private static void AddButton(TableLayoutPanel p,string t,int x,int y,Color color,Action a){var b=new Button{Text=t,Dock=DockStyle.Fill,BackColor=color,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Margin=new Padding(4)};b.FlatAppearance.BorderSize=0;b.Click+=(s,e)=>a();p.Controls.Add(b,x,y);}
  private void CreateUser(){try{UserManagementService.Create(username.Text,display.Text,Convert.ToString(role.SelectedItem),password.Text);password.Clear();status.Text="کاربر ایجاد شد.";RefreshAll();}catch(Exception ex){status.Text=ex.Message;}}
  private void RefreshAll(){try{users.DataSource=UserManagementService.GetUsers(); history.DataSource=EmailAdministrationService.GetHistory();status.Text="کاربران و تاریخچه ایمیل بروزرسانی شدند.";}catch(Exception ex){status.Text=ex.Message;}}
  private void LoadSelectedEmail(){if(users.CurrentRow!=null&&users.Columns.Contains("Email"))email.Text=Convert.ToString(users.CurrentRow.Cells["Email"].Value);}
  private void SaveEmail(){if(users.CurrentRow==null){status.Text="ابتدا کاربر را انتخاب کنید.";return;}try{EmailAdministrationService.UpdateUserEmail(Convert.ToInt32(users.CurrentRow.Cells["UserId"].Value),email.Text);RefreshAll();status.Text="ایمیل ذخیره شد.";}catch(Exception ex){status.Text=ex.Message;}}
  private void ToggleUser(){if(users.CurrentRow==null)return;try{bool active=Convert.ToBoolean(users.CurrentRow.Cells["IsActive"].Value);UserManagementService.SetActive(Convert.ToInt32(users.CurrentRow.Cells["UserId"].Value),!active);RefreshAll();}catch(Exception ex){status.Text=ex.Message;}}
  private void RetrySelectedEmail(){if(history.CurrentRow==null)return;try{if(Convert.ToString(history.CurrentRow.Cells["Status"].Value)!="DeadLetter")throw new InvalidOperationException("فقط پیام‌های DeadLetter قابل ارسال مجدد هستند.");EmailAdministrationService.RetryDeadLetter(Convert.ToInt64(history.CurrentRow.Cells["Id"].Value));RefreshAll();status.Text="پیام دوباره در صف قرار گرفت.";}catch(Exception ex){status.Text=ex.Message;}}
 }
}
