USE AshkanIndustrialAccounting;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code=N'2100')
BEGIN
    INSERT dbo.Accounts(Code,NameEn,NameFa,Type,IsActive)
    VALUES(N'2100',N'Sales tax payable',N'مالیات فروش پرداختنی',N'Liability',1);
END;
GO
