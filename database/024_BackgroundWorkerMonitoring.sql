-- Stage 94: worker heartbeat and operational monitoring.
IF OBJECT_ID(N'dbo.BackgroundWorkerStatus',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BackgroundWorkerStatus(
  WorkerName nvarchar(100) NOT NULL CONSTRAINT PK_BackgroundWorkerStatus PRIMARY KEY,
  LastHeartbeatUtc datetime2(3) NOT NULL,
  LastStatus nvarchar(20) NOT NULL CONSTRAINT DF_BackgroundWorkerStatus_Status DEFAULT(N'Healthy'),
  LastError nvarchar(1000) NULL,
  UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_BackgroundWorkerStatus_Updated DEFAULT(SYSUTCDATETIME()),
  CONSTRAINT CK_BackgroundWorkerStatus_Status CHECK(LastStatus IN(N'Healthy',N'Warning',N'Error'))
 );
END;
GO
