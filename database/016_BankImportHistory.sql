-- Stage 62: Run after 015_BankReconciliation.sql. Back up production data first.
IF OBJECT_ID(N'dbo.BankImportHistory', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BankImportHistory (
  ImportId uniqueidentifier NOT NULL PRIMARY KEY,
  CashBankAccountId int NOT NULL REFERENCES dbo.CashBankAccounts(Id),
  ImportedAt datetime2(3) NOT NULL CONSTRAINT DF_BankImportHistory_ImportedAt DEFAULT SYSUTCDATETIME(),
  ImportedBy nvarchar(100) NOT NULL,
  RowCount int NOT NULL CHECK (RowCount > 0 AND RowCount <= 10000),
  TotalIncoming decimal(22,2) NOT NULL CHECK (TotalIncoming >= 0),
  TotalOutgoing decimal(22,2) NOT NULL CHECK (TotalOutgoing >= 0)
 );
 CREATE INDEX IX_BankImportHistory_AccountTime ON dbo.BankImportHistory(CashBankAccountId,ImportedAt DESC);
END;
IF COL_LENGTH(N'dbo.BankStatementLines', N'ImportId') IS NULL
 ALTER TABLE dbo.BankStatementLines ADD ImportId uniqueidentifier NULL;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BankStatementLines_ImportHistory')
 ALTER TABLE dbo.BankStatementLines ADD CONSTRAINT FK_BankStatementLines_ImportHistory
 FOREIGN KEY(ImportId) REFERENCES dbo.BankImportHistory(ImportId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_BankStatementLines_ImportId' AND object_id=OBJECT_ID(N'dbo.BankStatementLines'))
 CREATE INDEX IX_BankStatementLines_ImportId ON dbo.BankStatementLines(ImportId) WHERE ImportId IS NOT NULL;
