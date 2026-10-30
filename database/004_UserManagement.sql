/* Stage 6 - SQL-backed user directory. Back up the database before applying. */
IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.AppUsers(
  UserId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUsers PRIMARY KEY,
  UserName nvarchar(80) NOT NULL,
  DisplayName nvarchar(120) NOT NULL,
  RoleName nvarchar(30) NOT NULL,
  PasswordSalt varbinary(32) NOT NULL,
  PasswordHash varbinary(32) NOT NULL,
  IsActive bit NOT NULL CONSTRAINT DF_AppUsers_IsActive DEFAULT(1),
  FailedLoginCount int NOT NULL CONSTRAINT DF_AppUsers_FailedLoginCount DEFAULT(0),
  LockedUntil datetime2 NULL,
  CreatedAt datetime2 NOT NULL CONSTRAINT DF_AppUsers_CreatedAt DEFAULT(SYSUTCDATETIME()),
  CONSTRAINT UQ_AppUsers_UserName UNIQUE(UserName),
  CONSTRAINT CK_AppUsers_Role CHECK(RoleName IN ('Administrator','Accountant','Manager'))
 );
END;
GO
