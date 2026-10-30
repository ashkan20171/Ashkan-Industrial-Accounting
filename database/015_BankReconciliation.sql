-- Stage 46: Apply after 014_CashBankTransfers.sql. Back up the database first.
IF OBJECT_ID(N'dbo.BankStatementLines',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BankStatementLines (
  Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  CashBankAccountId int NOT NULL REFERENCES dbo.CashBankAccounts(Id),
  StatementDate date NOT NULL,
  ExternalReference nvarchar(100) NOT NULL,
  Description nvarchar(500) NULL,
  Amount decimal(18,2) NOT NULL,
  MatchedPaymentId int NULL REFERENCES dbo.Payments(Id),
  MatchedTransferId int NULL REFERENCES dbo.CashBankTransfers(Id),
  ImportedBy nvarchar(100) NOT NULL,
  ImportedAt datetime2 NOT NULL CONSTRAINT DF_BankStatementLines_ImportedAt DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_BankStatementLines_Nonzero CHECK(Amount<>0),
  CONSTRAINT CK_BankStatementLines_OneMatch CHECK(MatchedPaymentId IS NULL OR MatchedTransferId IS NULL),
  CONSTRAINT UQ_BankStatementLines_External UNIQUE(CashBankAccountId,ExternalReference)
 );
 CREATE UNIQUE INDEX UX_BankStatementLines_PaymentMatch ON dbo.BankStatementLines(MatchedPaymentId) WHERE MatchedPaymentId IS NOT NULL;
 CREATE UNIQUE INDEX UX_BankStatementLines_TransferMatch ON dbo.BankStatementLines(MatchedTransferId) WHERE MatchedTransferId IS NOT NULL;
 CREATE INDEX IX_BankStatementLines_AccountDate ON dbo.BankStatementLines(CashBankAccountId,StatementDate);
END;
