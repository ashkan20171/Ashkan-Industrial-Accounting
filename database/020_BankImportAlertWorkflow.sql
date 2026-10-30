-- Stage 80: alert ownership, deadlines, notes and immutable workflow history.
IF COL_LENGTH(N'dbo.BankImportAlerts',N'AssignedToUserId') IS NULL
    ALTER TABLE dbo.BankImportAlerts ADD AssignedToUserId int NULL;
IF COL_LENGTH(N'dbo.BankImportAlerts',N'DueAtUtc') IS NULL
    ALTER TABLE dbo.BankImportAlerts ADD DueAtUtc datetime2(3) NULL;
IF COL_LENGTH(N'dbo.BankImportAlerts',N'PriorityCode') IS NULL
BEGIN
    ALTER TABLE dbo.BankImportAlerts ADD PriorityCode nvarchar(20) NOT NULL
        CONSTRAINT DF_BankImportAlerts_Priority DEFAULT N'Normal';
END;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_BankImportAlerts_Priority')
    ALTER TABLE dbo.BankImportAlerts ADD CONSTRAINT CK_BankImportAlerts_Priority
        CHECK(PriorityCode IN (N'Low',N'Normal',N'High',N'Urgent'));
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BankImportAlerts_AssignedUser')
    ALTER TABLE dbo.BankImportAlerts ADD CONSTRAINT FK_BankImportAlerts_AssignedUser
        FOREIGN KEY(AssignedToUserId) REFERENCES dbo.AppUsers(UserId);
IF OBJECT_ID(N'dbo.BankImportAlertNotes',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BankImportAlertNotes(
  Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BankImportAlertNotes PRIMARY KEY,
  AlertId int NOT NULL CONSTRAINT FK_BankImportAlertNotes_Alert REFERENCES dbo.BankImportAlerts(Id),
  NoteText nvarchar(1000) NOT NULL,
  CreatedByUserId int NOT NULL CONSTRAINT FK_BankImportAlertNotes_User REFERENCES dbo.AppUsers(UserId),
  CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_BankImportAlertNotes_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_BankImportAlertNotes_Text CHECK(LEN(LTRIM(RTRIM(NoteText))) BETWEEN 1 AND 1000)
 );
 CREATE INDEX IX_BankImportAlertNotes_Alert ON dbo.BankImportAlertNotes(AlertId,CreatedAtUtc DESC,Id DESC);
END;
IF OBJECT_ID(N'dbo.BankImportAlertHistory',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BankImportAlertHistory(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BankImportAlertHistory PRIMARY KEY,
  AlertId int NOT NULL CONSTRAINT FK_BankImportAlertHistory_Alert REFERENCES dbo.BankImportAlerts(Id),
  ChangedByUserId int NOT NULL CONSTRAINT FK_BankImportAlertHistory_User REFERENCES dbo.AppUsers(UserId),
  ChangeType nvarchar(30) NOT NULL,
  OldValue nvarchar(200) NULL, NewValue nvarchar(200) NULL,
  ChangedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_BankImportAlertHistory_Changed DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_BankImportAlertHistory_Type CHECK(ChangeType IN (N'Status',N'Assignment',N'DueDate',N'Priority'))
 );
 CREATE INDEX IX_BankImportAlertHistory_Alert ON dbo.BankImportAlertHistory(AlertId,ChangedAtUtc DESC,Id DESC);
END;
GO
