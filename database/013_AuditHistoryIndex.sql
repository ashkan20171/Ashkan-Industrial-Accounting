-- Optional performance index; run after a backup, outside business hours.
IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
 THROW 51301, 'AuditLog table is missing; apply 003_RolesAndAudit.sql first.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AuditLog') AND name=N'IX_AuditLog_UserActionTime')
 CREATE INDEX IX_AuditLog_UserActionTime ON dbo.AuditLog(UserName,ActionName,EventTime DESC)
 INCLUDE(EntityName,EntityId);
