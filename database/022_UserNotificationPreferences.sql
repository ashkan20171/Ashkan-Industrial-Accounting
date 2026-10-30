-- Stage 91: per-user in-app notification preferences.
IF OBJECT_ID(N'dbo.UserNotificationPreferences',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.UserNotificationPreferences(
  UserId int NOT NULL CONSTRAINT PK_UserNotificationPreferences PRIMARY KEY
   CONSTRAINT FK_UserNotificationPreferences_User REFERENCES dbo.AppUsers(UserId),
  EnableInApp bit NOT NULL CONSTRAINT DF_UserNotificationPreferences_InApp DEFAULT 1,
  NotifyCritical bit NOT NULL CONSTRAINT DF_UserNotificationPreferences_Critical DEFAULT 1,
  NotifyHigh bit NOT NULL CONSTRAINT DF_UserNotificationPreferences_High DEFAULT 1,
  NotifyMedium bit NOT NULL CONSTRAINT DF_UserNotificationPreferences_Medium DEFAULT 1,
  NotifyLow bit NOT NULL CONSTRAINT DF_UserNotificationPreferences_Low DEFAULT 1,
  UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_UserNotificationPreferences_Updated DEFAULT SYSUTCDATETIME()
 );
END;
GO
