-- Stage 88: transactional notification outbox and in-app notification inbox.
-- Run after database/020_BankImportAlertWorkflow.sql.
IF OBJECT_ID(N'dbo.BankAlertNotificationOutbox',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BankAlertNotificationOutbox(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BankAlertNotificationOutbox PRIMARY KEY,
  AlertId int NOT NULL CONSTRAINT FK_BankAlertNotificationOutbox_Alert REFERENCES dbo.BankImportAlerts(Id),
  EventKey nvarchar(120) NOT NULL,
  EventType nvarchar(40) NOT NULL,
  CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_BankAlertOutbox_Created DEFAULT SYSUTCDATETIME(),
  ProcessedAtUtc datetime2(3) NULL,
  AttemptCount int NOT NULL CONSTRAINT DF_BankAlertOutbox_Attempts DEFAULT 0,
  LastError nvarchar(1000) NULL,
  CONSTRAINT UQ_BankAlertNotificationOutbox_EventKey UNIQUE(EventKey)
 );
 CREATE INDEX IX_BankAlertNotificationOutbox_Pending ON dbo.BankAlertNotificationOutbox(ProcessedAtUtc,Id) INCLUDE(AlertId,EventType,AttemptCount);
END;
IF OBJECT_ID(N'dbo.UserNotifications',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.UserNotifications(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserNotifications PRIMARY KEY,
  UserId int NOT NULL CONSTRAINT FK_UserNotifications_User REFERENCES dbo.AppUsers(UserId),
  AlertId int NOT NULL CONSTRAINT FK_UserNotifications_Alert REFERENCES dbo.BankImportAlerts(Id),
  OutboxId bigint NOT NULL CONSTRAINT FK_UserNotifications_Outbox REFERENCES dbo.BankAlertNotificationOutbox(Id),
  Title nvarchar(200) NOT NULL,
  Message nvarchar(1000) NOT NULL,
  SeverityCode nvarchar(20) NOT NULL,
  CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_UserNotifications_Created DEFAULT SYSUTCDATETIME(),
  ReadAtUtc datetime2(3) NULL,
  CONSTRAINT UQ_UserNotifications_OutboxUser UNIQUE(OutboxId,UserId)
 );
 CREATE INDEX IX_UserNotifications_Inbox ON dbo.UserNotifications(UserId,ReadAtUtc,CreatedAtUtc DESC);
END;
GO
