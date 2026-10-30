USE AshkanIndustrialAccounting;
GO
IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.AppUsers (
  Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  UserName NVARCHAR(100) NOT NULL UNIQUE,
  PasswordHash VARBINARY(32) NOT NULL,
  PasswordSalt VARBINARY(32) NOT NULL,
  PasswordIterations INT NOT NULL,
  RoleName NVARCHAR(20) NOT NULL CONSTRAINT CK_AppUsers_Role CHECK(RoleName IN ('Administrator','Accountant','Manager')),
  IsActive BIT NOT NULL DEFAULT 1,
  FailedLoginCount INT NOT NULL DEFAULT 0,
  LockedUntilUtc DATETIME2 NULL,
  CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
 );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditLog') AND name = N'IX_AuditLog_EventTime')
 CREATE INDEX IX_AuditLog_EventTime ON dbo.AuditLog(EventTime DESC);
GO
-- No default users/passwords are seeded. Existing local administrator credentials
-- are NOT migrated automatically. Implement and test an explicit migration flow
-- before enabling SQL-backed login in production.
