IF DB_ID(N'AshkanIndustrialAccounting') IS NULL CREATE DATABASE AshkanIndustrialAccounting;
GO
USE AshkanIndustrialAccounting;
GO
IF OBJECT_ID('dbo.Accounts') IS NULL
CREATE TABLE dbo.Accounts(
 Id INT IDENTITY PRIMARY KEY, Code NVARCHAR(30) NOT NULL UNIQUE, NameEn NVARCHAR(150) NOT NULL,
 NameFa NVARCHAR(150) NOT NULL DEFAULT N'', Type NVARCHAR(30) NOT NULL, IsActive BIT NOT NULL DEFAULT 1,
 CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.CostCenters') IS NULL
CREATE TABLE dbo.CostCenters(
 Id INT IDENTITY PRIMARY KEY, Code NVARCHAR(30) NOT NULL UNIQUE, Name NVARCHAR(150) NOT NULL,
 Budget DECIMAL(18,2) NOT NULL DEFAULT 0, ActualCost DECIMAL(18,2) NOT NULL DEFAULT 0, IsActive BIT NOT NULL DEFAULT 1
);
IF OBJECT_ID('dbo.JournalEntries') IS NULL
CREATE TABLE dbo.JournalEntries(
 Id INT IDENTITY PRIMARY KEY, EntryNo NVARCHAR(40) NOT NULL UNIQUE, EntryDate DATETIME2 NOT NULL,
 Description NVARCHAR(500) NULL, ReferenceNo NVARCHAR(80) NULL, IsPosted BIT NOT NULL DEFAULT 0,
 CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.JournalLines') IS NULL
CREATE TABLE dbo.JournalLines(
 Id INT IDENTITY PRIMARY KEY, JournalEntryId INT NOT NULL REFERENCES dbo.JournalEntries(Id),
 AccountId INT NOT NULL REFERENCES dbo.Accounts(Id), Description NVARCHAR(500) NULL,
 Debit DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK(Debit>=0), Credit DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK(Credit>=0),
 CostCenterId INT NULL REFERENCES dbo.CostCenters(Id),
 CONSTRAINT CK_JournalLine_OneSide CHECK ((Debit > 0 AND Credit = 0) OR (Credit > 0 AND Debit = 0))
);
IF OBJECT_ID('dbo.AuditLog') IS NULL
CREATE TABLE dbo.AuditLog(
 Id BIGINT IDENTITY PRIMARY KEY, EventTime DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 UserName NVARCHAR(100) NOT NULL, ActionName NVARCHAR(100) NOT NULL, EntityName NVARCHAR(100) NULL,
 EntityId NVARCHAR(100) NULL, Details NVARCHAR(MAX) NULL
);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.Accounts)
BEGIN
 INSERT dbo.Accounts(Code,NameEn,NameFa,Type) VALUES
 ('1000','Cash','صندوق','Asset'),('1100','Bank','بانک','Asset'),('1200','Accounts Receivable','حساب‌های دریافتنی','Asset'),
 ('2000','Accounts Payable','حساب‌های پرداختنی','Liability'),('3000','Owner Equity','حقوق مالکانه','Equity'),
 ('4000','Sales Revenue','درآمد فروش','Revenue'),('5000','Cost of Goods Sold','بهای تمام‌شده کالای فروش‌رفته','Expense'),
 ('5100','Direct Materials','مواد مستقیم','Expense'),('5200','Direct Labor','دستمزد مستقیم','Expense'),('5300','Manufacturing Overhead','سربار ساخت','Expense');
 INSERT dbo.CostCenters(Code,Name,Budget,ActualCost) VALUES
 ('CC-PROD','Production',500000,420000),('CC-QA','Quality Assurance',120000,98000),('CC-MNT','Maintenance',180000,172000),('CC-WH','Warehouse',95000,87000);
END
GO
