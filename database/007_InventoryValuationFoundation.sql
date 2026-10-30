USE AshkanIndustrialAccounting;
GO
-- Independent valuation ledger. Existing stock quantities are NOT migrated automatically.
IF OBJECT_ID(N'dbo.InventoryValuations',N'U') IS NULL
CREATE TABLE dbo.InventoryValuations(
 ProductId INT NOT NULL PRIMARY KEY REFERENCES dbo.Products(Id),
 Quantity DECIMAL(18,3) NOT NULL DEFAULT 0 CHECK(Quantity>=0),
 TotalValue DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK(TotalValue>=0),
 RowVersion ROWVERSION NOT NULL
);
GO
IF OBJECT_ID(N'dbo.InventoryValuationEvents',N'U') IS NULL
CREATE TABLE dbo.InventoryValuationEvents(
 Id BIGINT IDENTITY PRIMARY KEY,
 OperationId UNIQUEIDENTIFIER NOT NULL UNIQUE,
 ProductId INT NOT NULL REFERENCES dbo.Products(Id),
 EventType VARCHAR(12) NOT NULL CHECK(EventType IN ('OPENING','RECEIPT','ISSUE','RETURN')),
 QuantityDelta DECIMAL(18,3) NOT NULL,
 ValueDelta DECIMAL(18,2) NOT NULL,
 QuantityAfter DECIMAL(18,3) NOT NULL CHECK(QuantityAfter>=0),
 ValueAfter DECIMAL(18,2) NOT NULL CHECK(ValueAfter>=0),
 RecordedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
