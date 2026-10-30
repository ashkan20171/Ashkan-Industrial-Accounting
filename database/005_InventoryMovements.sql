USE AshkanIndustrialAccounting;
GO
IF OBJECT_ID(N'dbo.InventoryMovements',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.InventoryMovements(
  Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  OperationId UNIQUEIDENTIFIER NOT NULL UNIQUE,
  ProductId INT NOT NULL REFERENCES dbo.Products(Id),
  MovementType VARCHAR(10) NOT NULL CHECK (MovementType IN ('IN','OUT','ADJUST')),
  QuantityDelta DECIMAL(18,3) NOT NULL CHECK (QuantityDelta <> 0),
  StockBefore DECIMAL(18,3) NOT NULL,
  StockAfter DECIMAL(18,3) NOT NULL CHECK (StockAfter >= 0),
  ReferenceNo NVARCHAR(80) NULL,
  Notes NVARCHAR(500) NULL,
  PerformedBy NVARCHAR(100) NOT NULL,
  CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_InventoryMovementDirection CHECK ((MovementType='IN' AND QuantityDelta>0) OR (MovementType='OUT' AND QuantityDelta<0) OR MovementType='ADJUST')
 );
 CREATE INDEX IX_InventoryMovements_ProductDate ON dbo.InventoryMovements(ProductId,CreatedAtUtc DESC);
END;
GO
