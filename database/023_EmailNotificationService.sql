-- Stage 92: independently processed email notification queue.
IF COL_LENGTH(N'dbo.AppUsers',N'Email') IS NULL
    ALTER TABLE dbo.AppUsers ADD Email nvarchar(254) NULL;
GO
IF COL_LENGTH(N'dbo.UserNotificationPreferences',N'EnableEmail') IS NULL
    ALTER TABLE dbo.UserNotificationPreferences ADD EnableEmail bit NOT NULL CONSTRAINT DF_UserNotificationPreferences_Email DEFAULT(0);
GO
IF OBJECT_ID(N'dbo.EmailNotificationOutbox',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.EmailNotificationOutbox(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmailNotificationOutbox PRIMARY KEY,
  UserId int NOT NULL CONSTRAINT FK_EmailNotificationOutbox_User REFERENCES dbo.AppUsers(UserId),
  NotificationId bigint NOT NULL CONSTRAINT FK_EmailNotificationOutbox_Notification REFERENCES dbo.UserNotifications(Id),
  RecipientEmail nvarchar(254) NOT NULL,
  Subject nvarchar(200) NOT NULL,
  Body nvarchar(max) NOT NULL,
  Status nvarchar(20) NOT NULL CONSTRAINT DF_EmailNotificationOutbox_Status DEFAULT(N'Pending'),
  AttemptCount int NOT NULL CONSTRAINT DF_EmailNotificationOutbox_Attempts DEFAULT(0),
  NextAttemptAtUtc datetime2(3) NOT NULL CONSTRAINT DF_EmailNotificationOutbox_Next DEFAULT(SYSUTCDATETIME()),
  CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_EmailNotificationOutbox_Created DEFAULT(SYSUTCDATETIME()),
  SentAtUtc datetime2(3) NULL,
  LastError nvarchar(1000) NULL,
  CONSTRAINT UQ_EmailNotificationOutbox_Notification UNIQUE(NotificationId),
  CONSTRAINT CK_EmailNotificationOutbox_Status CHECK(Status IN(N'Pending',N'Sent',N'DeadLetter'))
 );
 CREATE INDEX IX_EmailNotificationOutbox_Queue ON dbo.EmailNotificationOutbox(Status,NextAttemptAtUtc,Id);
END;
GO
