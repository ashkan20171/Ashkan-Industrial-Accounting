-- Stage 71: Apply after 017. Review index costs and back up before production deployment.
IF OBJECT_ID(N'dbo.BankImportHistory', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BankImportHistory') AND name=N'IX_BankImportHistory_AccountDateCover')
 CREATE INDEX IX_BankImportHistory_AccountDateCover
 ON dbo.BankImportHistory(CashBankAccountId, ImportedAt DESC, ImportId DESC)
 INCLUDE (ImportedBy, RowCount, TotalIncoming, TotalOutgoing);
GO
IF OBJECT_ID(N'dbo.BankImportFailureAudit', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BankImportFailureAudit') AND name=N'IX_BankImportFailureAudit_DateAccount')
 CREATE INDEX IX_BankImportFailureAudit_DateAccount
 ON dbo.BankImportFailureAudit(AttemptedAt, CashBankAccountId)
 INCLUDE (FailureCode);
GO
