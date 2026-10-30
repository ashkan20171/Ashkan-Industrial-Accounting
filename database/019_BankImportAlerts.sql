-- Stage 79: persistent bank import alert workflow. Back up the database before deployment.
IF OBJECT_ID(N'dbo.BankImportAlerts',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BankImportAlerts
 (
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BankImportAlerts PRIMARY KEY,
  CashBankAccountId int NOT NULL CONSTRAINT FK_BankImportAlerts_CashBankAccounts REFERENCES dbo.CashBankAccounts(Id),
  PeriodFrom date NOT NULL,
  PeriodUntil date NOT NULL,
  SeverityCode nvarchar(20) NOT NULL CHECK (SeverityCode IN (N'Critical',N'Warning',N'Information')),
  SuccessfulImports bigint NOT NULL CHECK (SuccessfulImports>=0),
  FailedImports bigint NOT NULL CHECK (FailedImports>0),
  FailureRate decimal(7,2) NOT NULL CHECK (FailureRate BETWEEN 0 AND 100),
  Recommendation nvarchar(500) NOT NULL,
  StatusCode nvarchar(20) NOT NULL CONSTRAINT DF_BankImportAlerts_Status DEFAULT N'New'
    CHECK (StatusCode IN (N'New',N'InProgress',N'Resolved')),
  FirstDetectedAt datetime2(3) NOT NULL,
  LastDetectedAt datetime2(3) NOT NULL,
  ResolvedAt datetime2(3) NULL,
  ResolvedBy nvarchar(100) NULL,
  CONSTRAINT CK_BankImportAlerts_Period CHECK (PeriodFrom<PeriodUntil)
 );
 CREATE UNIQUE INDEX UX_BankImportAlerts_Scope ON dbo.BankImportAlerts(CashBankAccountId,PeriodFrom,PeriodUntil,SeverityCode);
 CREATE INDEX IX_BankImportAlerts_StatusSeverity ON dbo.BankImportAlerts(StatusCode,SeverityCode,LastDetectedAt DESC);
END;
GO
