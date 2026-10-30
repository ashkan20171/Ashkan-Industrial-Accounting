using System;
using System.Configuration;
using System.Data.SqlClient;

namespace AshkanIndustrialAccounting.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void EnsureCreated()
        {
            System.Exception last = null;
            foreach (var configured in Db.CandidateConnectionStrings())
            {
                try
                {
                    var builder = new SqlConnectionStringBuilder(configured);
                    var databaseName = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "AshkanIndustrialAccounting" : builder.InitialCatalog;
                    builder.InitialCatalog = "master";
                    using (var master = new SqlConnection(builder.ConnectionString))
                    {
                        master.Open();
                        using (var cmd = new SqlCommand("IF DB_ID(@db) IS NULL EXEC('CREATE DATABASE [' + REPLACE(@db,']',']]') + ']')", master))
                        {
                            cmd.Parameters.AddWithValue("@db", databaseName);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    builder.InitialCatalog = databaseName;
                    Db.UseConnectionString(builder.ConnectionString);
                    using (var cn = Db.Open())
                    {
                        EnsureSchema(cn);
                    }
                    return;
                }
                catch (System.Exception ex) { last = ex; }
            }
            throw new System.InvalidOperationException(
                "Could not connect to LocalDB, SQL Express, or localhost SQL Server. " +
                "Install SQL Server Express LocalDB or update App.config. Last error: " +
                (last == null ? "Unknown" : last.Message), last);
        }

        private static void EnsureSchema(SqlConnection cn)
        {
                Execute(cn, @"IF OBJECT_ID('dbo.Accounts') IS NULL
CREATE TABLE dbo.Accounts(
 Id INT IDENTITY PRIMARY KEY,
 Code NVARCHAR(30) NOT NULL UNIQUE,
 NameEn NVARCHAR(150) NOT NULL,
 NameFa NVARCHAR(150) NOT NULL DEFAULT N'',
 Type NVARCHAR(30) NOT NULL,
 IsActive BIT NOT NULL DEFAULT 1,
 CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);");

                Execute(cn, @"IF OBJECT_ID('dbo.CostCenters') IS NULL
CREATE TABLE dbo.CostCenters(
 Id INT IDENTITY PRIMARY KEY,
 Code NVARCHAR(30) NOT NULL UNIQUE,
 Name NVARCHAR(150) NOT NULL,
 Budget DECIMAL(18,2) NOT NULL DEFAULT 0,
 ActualCost DECIMAL(18,2) NOT NULL DEFAULT 0,
 IsActive BIT NOT NULL DEFAULT 1
);");

                Execute(cn, @"IF OBJECT_ID('dbo.JournalEntries') IS NULL
CREATE TABLE dbo.JournalEntries(
 Id INT IDENTITY PRIMARY KEY,
 EntryNo NVARCHAR(40) NOT NULL UNIQUE,
 EntryDate DATETIME2 NOT NULL,
 Description NVARCHAR(500) NULL,
 ReferenceNo NVARCHAR(80) NULL,
 IsPosted BIT NOT NULL DEFAULT 0,
 CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);");

                Execute(cn, @"IF OBJECT_ID('dbo.JournalLines') IS NULL
CREATE TABLE dbo.JournalLines(
 Id INT IDENTITY PRIMARY KEY,
 JournalEntryId INT NOT NULL REFERENCES dbo.JournalEntries(Id),
 AccountId INT NOT NULL REFERENCES dbo.Accounts(Id),
 Description NVARCHAR(500) NULL,
 Debit DECIMAL(18,2) NOT NULL DEFAULT 0,
 Credit DECIMAL(18,2) NOT NULL DEFAULT 0,
 CostCenterId INT NULL REFERENCES dbo.CostCenters(Id)
);");

                Execute(cn, @"IF OBJECT_ID('dbo.BusinessPartners') IS NULL
CREATE TABLE dbo.BusinessPartners(
 Id INT IDENTITY PRIMARY KEY,
 Code NVARCHAR(30) NOT NULL UNIQUE,
 Name NVARCHAR(180) NOT NULL,
 PartnerType NVARCHAR(20) NOT NULL,
 Phone NVARCHAR(50) NULL,
 Email NVARCHAR(150) NULL,
 TaxId NVARCHAR(60) NULL,
 CreditLimit DECIMAL(18,2) NOT NULL DEFAULT 0,
 IsActive BIT NOT NULL DEFAULT 1,
 CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);");

                Execute(cn, @"IF OBJECT_ID('dbo.Products') IS NULL
CREATE TABLE dbo.Products(
 Id INT IDENTITY PRIMARY KEY, SKU NVARCHAR(50) NOT NULL UNIQUE, Name NVARCHAR(180) NOT NULL,
 Unit NVARCHAR(30) NOT NULL DEFAULT N'عدد', SalePrice DECIMAL(18,2) NOT NULL DEFAULT 0,
 PurchasePrice DECIMAL(18,2) NOT NULL DEFAULT 0, StockQty DECIMAL(18,3) NOT NULL DEFAULT 0,
 ReorderPoint DECIMAL(18,3) NOT NULL DEFAULT 0, IsActive BIT NOT NULL DEFAULT 1
);");

                Execute(cn, @"IF OBJECT_ID('dbo.SalesInvoices') IS NULL
CREATE TABLE dbo.SalesInvoices(
 Id INT IDENTITY PRIMARY KEY, InvoiceNo NVARCHAR(40) NOT NULL UNIQUE, InvoiceDate DATETIME2 NOT NULL,
 PartnerId INT NOT NULL REFERENCES dbo.BusinessPartners(Id), CurrencyCode NVARCHAR(10) NOT NULL DEFAULT 'IRR',
 Subtotal DECIMAL(18,2) NOT NULL, Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
 Tax DECIMAL(18,2) NOT NULL DEFAULT 0, Total DECIMAL(18,2) NOT NULL, Status NVARCHAR(20) NOT NULL DEFAULT 'Draft'
);");

                Execute(cn, @"IF OBJECT_ID('dbo.SalesInvoiceLines') IS NULL
CREATE TABLE dbo.SalesInvoiceLines(
 Id INT IDENTITY PRIMARY KEY, InvoiceId INT NOT NULL REFERENCES dbo.SalesInvoices(Id),
 ProductId INT NOT NULL REFERENCES dbo.Products(Id), Qty DECIMAL(18,3) NOT NULL,
 UnitPrice DECIMAL(18,2) NOT NULL, Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
 LineTotal DECIMAL(18,2) NOT NULL
);");

                Execute(cn, @"IF OBJECT_ID('dbo.CashBankAccounts') IS NULL
CREATE TABLE dbo.CashBankAccounts(
 Id INT IDENTITY PRIMARY KEY, Code NVARCHAR(30) NOT NULL UNIQUE, Name NVARCHAR(150) NOT NULL,
 AccountType NVARCHAR(20) NOT NULL, CurrencyCode NVARCHAR(10) NOT NULL DEFAULT 'IRR',
 Balance DECIMAL(18,2) NOT NULL DEFAULT 0, IsActive BIT NOT NULL DEFAULT 1
);");

                Execute(cn, @"IF OBJECT_ID('dbo.PurchaseInvoices') IS NULL
CREATE TABLE dbo.PurchaseInvoices(
 Id INT IDENTITY PRIMARY KEY, InvoiceNo NVARCHAR(40) NOT NULL UNIQUE, InvoiceDate DATETIME2 NOT NULL,
 PartnerId INT NOT NULL REFERENCES dbo.BusinessPartners(Id), CurrencyCode NVARCHAR(10) NOT NULL DEFAULT 'IRR',
 Total DECIMAL(18,2) NOT NULL DEFAULT 0, Status NVARCHAR(20) NOT NULL DEFAULT 'Draft'
);");

                Execute(cn, @"IF OBJECT_ID('dbo.Payments') IS NULL
CREATE TABLE dbo.Payments(
 Id INT IDENTITY PRIMARY KEY, PaymentNo NVARCHAR(40) NOT NULL UNIQUE, PaymentDate DATETIME2 NOT NULL,
 Direction NVARCHAR(20) NOT NULL, PartnerId INT NULL REFERENCES dbo.BusinessPartners(Id),
 CashBankAccountId INT NOT NULL REFERENCES dbo.CashBankAccounts(Id),
 Amount DECIMAL(18,2) NOT NULL, CurrencyCode NVARCHAR(10) NOT NULL DEFAULT 'IRR',
 ReferenceNo NVARCHAR(80) NULL, Description NVARCHAR(500) NULL
);");

                Execute(cn, @"IF OBJECT_ID('dbo.AuditLog') IS NULL
CREATE TABLE dbo.AuditLog(
 Id BIGINT IDENTITY PRIMARY KEY,
 EventTime DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 UserName NVARCHAR(100) NOT NULL,
 ActionName NVARCHAR(100) NOT NULL,
 EntityName NVARCHAR(100) NULL,
 EntityId NVARCHAR(100) NULL,
 Details NVARCHAR(MAX) NULL
);");

                Execute(cn, @"IF NOT EXISTS(SELECT 1 FROM dbo.Accounts)
BEGIN
 INSERT dbo.Accounts(Code,NameEn,NameFa,Type) VALUES
 ('1000','Cash',N'صندوق','Asset'),
 ('1100','Bank',N'بانک','Asset'),
 ('1200','Accounts Receivable',N'حساب‌های دریافتنی','Asset'),
 ('2000','Accounts Payable',N'حساب‌های پرداختنی','Liability'),
 ('3000','Owner Equity',N'حقوق مالکانه','Equity'),
 ('4000','Sales Revenue',N'درآمد فروش','Revenue'),
 ('5000','Cost of Goods Sold',N'بهای تمام‌شده کالای فروش‌رفته','Expense'),
 ('5100','Direct Materials',N'مواد مستقیم','Expense'),
 ('5200','Direct Labor',N'دستمزد مستقیم','Expense'),
 ('5300','Manufacturing Overhead',N'سربار ساخت','Expense');
END");

                Execute(cn, @"IF NOT EXISTS(SELECT 1 FROM dbo.Products)
BEGIN
 INSERT dbo.Products(SKU,Name,Unit,SalePrice,PurchasePrice,StockQty,ReorderPoint) VALUES
 ('RM-001',N'ورق فولادی','kg',850000,720000,1250,300),
 ('RM-002',N'بلبرینگ صنعتی','pcs',4200000,3650000,86,20),
 ('FG-001',N'پمپ صنعتی مدل A','pcs',185000000,142000000,18,5),
 ('FG-002',N'شیر صنعتی مدل V','pcs',96000000,74000000,32,8);
END");

                Execute(cn, @"IF NOT EXISTS(SELECT 1 FROM dbo.CashBankAccounts)
BEGIN
 INSERT dbo.CashBankAccounts(Code,Name,AccountType,CurrencyCode,Balance) VALUES
 ('CASH-IRR',N'صندوق مرکزی','Cash','IRR',2450000000),
 ('BANK-IRR',N'حساب بانکی عملیاتی','Bank','IRR',12850000000),
 ('BANK-USD','Foreign Currency Bank','Bank','USD',125000);
END");
        }

        private static void Execute(SqlConnection cn, string sql)
        {
            using (var cmd = new SqlCommand(sql, cn)) cmd.ExecuteNonQuery();
        }
    }
}