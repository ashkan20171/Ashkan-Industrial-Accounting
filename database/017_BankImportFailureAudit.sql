-- Stage 64. Run after 016_BankImportHistory.sql. Back up production database first.
IF OBJECT_ID(N'dbo.BankImportFailureAudit',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BankImportFailureAudit (
  Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
  CashBankAccountId int NOT NULL REFERENCES dbo.CashBankAccounts(Id),
  AttemptedAt datetime2(3) NOT NULL CONSTRAINT DF_BankImportFailureAudit_AttemptedAt DEFAULT SYSUTCDATETIME(),
  AttemptedBy nvarchar(100) NOT NULL,
  AttemptedRows int NOT NULL CHECK (AttemptedRows BETWEEN 0 AND 10000),
  FailureCode nvarchar(40) NOT NULL CHECK (FailureCode IN (N'DuplicateReference',N'DatabaseError',N'ValidationError'))
 );
 CREATE INDEX IX_BankImportFailureAudit_AccountTime ON dbo.BankImportFailureAudit(CashBankAccountId,AttemptedAt DESC);
END;
