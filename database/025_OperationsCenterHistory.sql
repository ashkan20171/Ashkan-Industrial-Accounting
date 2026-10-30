-- Stage 95: durable worker heartbeat history for the Operations Center.
IF OBJECT_ID(N'dbo.BackgroundWorkerHistory',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BackgroundWorkerHistory(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BackgroundWorkerHistory PRIMARY KEY,
  WorkerName nvarchar(100) NOT NULL,
  HeartbeatUtc datetime2(3) NOT NULL CONSTRAINT DF_BackgroundWorkerHistory_Heartbeat DEFAULT(SYSUTCDATETIME()),
  Status nvarchar(20) NOT NULL,
  ErrorMessage nvarchar(1000) NULL,
  CONSTRAINT CK_BackgroundWorkerHistory_Status CHECK(Status IN(N'Healthy',N'Warning',N'Error'))
 );
 CREATE INDEX IX_BackgroundWorkerHistory_Heartbeat ON dbo.BackgroundWorkerHistory(HeartbeatUtc DESC,WorkerName) INCLUDE(Status);
END;
GO
