USE AshkanIndustrialAccounting;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code=N'1300')
 INSERT dbo.Accounts(Code,NameEn,NameFa,Type,IsActive)
 VALUES(N'1300',N'Inventory',N'موجودی کالا',N'Asset',1);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code=N'1390')
 INSERT dbo.Accounts(Code,NameEn,NameFa,Type,IsActive)
 VALUES(N'1390',N'Inventory Clearing',N'حساب واسط گردش انبار',N'Asset',1);
GO
