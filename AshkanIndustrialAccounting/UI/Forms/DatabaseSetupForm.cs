using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;
namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class DatabaseSetupForm : Form
    {
        private readonly TextBox _server=new TextBox(),_database=new TextBox();
        private readonly Label _result=new Label();
        public DatabaseSetupForm()
        {
            Text=LocalizationService.IsPersian?"تنظیم اتصال دیتابیس":"Database Connection";
            Width=570;Height=390;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
            var p=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(28),ColumnCount=2,RowCount=6};
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,32));p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,68));
            _server.Text=@"(LocalDB)\MSSQLLocalDB";_database.Text="AshkanIndustrialAccounting";
            Add(p,0,LocalizationService.IsPersian?"SQL Server":"SQL Server",_server);
            Add(p,1,LocalizationService.IsPersian?"نام دیتابیس":"Database",_database);
            var test=new Button{Text=LocalizationService.IsPersian?"تست و ایجاد دیتابیس":"Test & Create Database",Dock=DockStyle.Fill,Height=42,BackColor=Color.FromArgb(25,99,180),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
            test.Click+=(s,e)=>Test();
            p.Controls.Add(test,1,2);
            _result.Dock=DockStyle.Fill;_result.AutoSize=true;p.Controls.Add(_result,0,3);p.SetColumnSpan(_result,2);
            var hint=new Label{Dock=DockStyle.Fill,AutoSize=true,ForeColor=Color.DimGray,Text=LocalizationService.IsPersian?
                "نمونه‌ها: (LocalDB)\\MSSQLLocalDB  یا  .\\SQLEXPRESS  یا نام سرور SQL شما":"Examples: (LocalDB)\\MSSQLLocalDB, .\\SQLEXPRESS, or your SQL Server name"};
            p.Controls.Add(hint,0,4);p.SetColumnSpan(hint,2);Controls.Add(p);
        }
        private void Test()
        {
            try{
                var b=new SqlConnectionStringBuilder{DataSource=_server.Text.Trim(),InitialCatalog="master",IntegratedSecurity=true,ConnectTimeout=5};
                if(!_server.Text.Contains("LocalDB")) b.TrustServerCertificate=true;
                using(var cn=new SqlConnection(b.ConnectionString)){cn.Open();using(var cmd=new SqlCommand("IF DB_ID(@db) IS NULL EXEC('CREATE DATABASE ['+REPLACE(@db,']',']]')+']')",cn)){cmd.Parameters.AddWithValue("@db",_database.Text.Trim());cmd.ExecuteNonQuery();}}
                b.InitialCatalog=_database.Text.Trim();Db.UseConnectionString(b.ConnectionString);DbInitializer.EnsureCreated();
                _result.ForeColor=Color.SeaGreen;_result.Text=LocalizationService.IsPersian?"✓ اتصال برقرار شد و دیتابیس آماده است.":"✓ Connection successful. Database is ready.";
            }catch(Exception ex){_result.ForeColor=Color.Firebrick;_result.Text=ex.Message;}
        }
        private static void Add(TableLayoutPanel p,int row,string l,Control c){p.RowStyles.Add(new RowStyle(SizeType.Absolute,52));p.Controls.Add(new Label{Text=l,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,row);c.Dock=DockStyle.Fill;p.Controls.Add(c,1,row);}
    }
}