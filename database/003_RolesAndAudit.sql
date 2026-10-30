USE AshkanIndustrialAccounting;
GO
IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.AuditLog(
  Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  EventTime DATETIME2 NOT NULL CONSTRAINT DF_AuditLog_EventTime DEFAULT SYSUTCDATETIME(),
  UserName NVARCHAR(100) NOT NULL,
  ActionName NVARCHAR(100) NOT NULL,
  EntityName NVARCHAR(100) NULL,
  EntityId NVARCHAR(100) NULL,
  Details NVARCHAR(MAX) NULL
 );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AuditLog') AND name=N'IX_AuditLog_EventTime')
 CREATE INDEX IX_AuditLog_EventTime ON dbo.AuditLog(EventTime DESC);
GO
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RolePermissions(
  Id INT IDENTITY(1,1) PRIMARY KEY,
  RoleName NVARCHAR(20) NOT NULL,
  FeatureKey NVARCHAR(50) NOT NULL,
  CanAccess BIT NOT NULL,
  CONSTRAINT UQ_RolePermissions_Role_Feature UNIQUE(RoleName, FeatureKey),
  CONSTRAINT CK_RolePermissions_Role CHECK(RoleName IN ('Administrator','Accountant','Manager'))
 );
END;
GO
MERGE dbo.RolePermissions AS target
USING (VALUES
 ('Administrator','dashboard',1),('Administrator','sales',1),('Administrator','products',1),('Administrator','cashbank',1),('Administrator','accounts',1),('Administrator','journal',1),('Administrator','partners',1),('Administrator','costing',1),('Administrator','reports',1),('Administrator','settings',1),('Administrator','database',1),('Administrator','language',1),
 ('Accountant','dashboard',1),('Accountant','sales',1),('Accountant','products',1),('Accountant','cashbank',1),('Accountant','accounts',1),('Accountant','journal',1),('Accountant','partners',1),('Accountant','costing',1),('Accountant','reports',1),('Accountant','settings',0),('Accountant','database',0),('Accountant','language',1),
 ('Manager','dashboard',1),('Manager','sales',1),('Manager','products',1),('Manager','cashbank',1),('Manager','accounts',1),('Manager','journal',1),('Manager','partners',1),('Manager','costing',1),('Manager','reports',1),('Manager','settings',0),('Manager','database',0),('Manager','language',1)
) AS source(RoleName,FeatureKey,CanAccess) ON target.RoleName=source.RoleName AND target.FeatureKey=source.FeatureKey
WHEN MATCHED THEN UPDATE SET CanAccess=source.CanAccess
WHEN NOT MATCHED THEN INSERT(RoleName,FeatureKey,CanAccess) VALUES(source.RoleName,source.FeatureKey,source.CanAccess);
GO
-- Note: the current sign-in flow still authenticates the local bootstrap administrator.
-- SQL-backed multi-user authentication must be implemented before treating RolePermissions as enforcement.
