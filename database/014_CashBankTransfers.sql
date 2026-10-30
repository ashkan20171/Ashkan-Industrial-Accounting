-- Apply after backup. Cash/bank accounts must already exist.
IF OBJECT_ID(N'dbo.CashBankTransfers',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CashBankTransfers(
  Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  RequestKey nvarchar(80) NOT NULL,
  SourceAccountId int NOT NULL REFERENCES dbo.CashBankAccounts(Id),
  DestinationAccountId int NOT NULL REFERENCES dbo.CashBankAccounts(Id),
  Amount decimal(18,2) NOT NULL,
  CurrencyCode nvarchar(10) NOT NULL,
  Note nvarchar(500) NULL,
  CreatedBy nvarchar(100) NOT NULL,
  CreatedAt datetime2 NOT NULL CONSTRAINT DF_CashBankTransfers_CreatedAt DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_CashBankTransfers_Distinct CHECK(SourceAccountId<>DestinationAccountId),
  CONSTRAINT CK_CashBankTransfers_Positive CHECK(Amount>0),
  CONSTRAINT CK_CashBankTransfers_IRR CHECK(CurrencyCode=N'IRR'),
  CONSTRAINT UQ_CashBankTransfers_RequestKey UNIQUE(RequestKey)
 );
 CREATE INDEX IX_CashBankTransfers_SourceDate ON dbo.CashBankTransfers(SourceAccountId,CreatedAt);
 CREATE INDEX IX_CashBankTransfers_DestDate ON dbo.CashBankTransfers(DestinationAccountId,CreatedAt);
END;
