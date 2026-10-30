USE AshkanIndustrialAccounting;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code=N'1300')
 INSERT dbo.Accounts(Code,NameEn,NameFa,Type,IsActive) VALUES(N'1300',N'Inventory',N'موجودی کالا',N'Asset',1);
GO
IF OBJECT_ID(N'dbo.SalesCostSnapshots',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SalesCostSnapshots(
 InvoiceId INT NOT NULL,
 ProductId INT NOT NULL,
 Quantity DECIMAL(18,3) NOT NULL CHECK(Quantity>0),
 UnitCost DECIMAL(18,2) NOT NULL CHECK(UnitCost>=0),
 TotalCost DECIMAL(18,2) NOT NULL CHECK(TotalCost>=0),
 RecordedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT PK_SalesCostSnapshots PRIMARY KEY(InvoiceId,ProductId),
 CONSTRAINT FK_SalesCostSnapshots_Invoice FOREIGN KEY(InvoiceId) REFERENCES dbo.SalesInvoices(Id),
 CONSTRAINT FK_SalesCostSnapshots_Product FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id)
 );
END;
GO
