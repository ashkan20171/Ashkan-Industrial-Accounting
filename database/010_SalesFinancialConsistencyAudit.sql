USE AshkanIndustrialAccounting;
GO
-- Read-only diagnostic: review results before enabling accounting on historical data.
SELECT Id,InvoiceNo,Subtotal,Discount,Tax,Total,
       (Subtotal-Discount+Tax) AS ExpectedTotal,
       Total-(Subtotal-Discount+Tax) AS Difference
FROM dbo.SalesInvoices
WHERE ABS(Total-(Subtotal-Discount+Tax))>0.01
   OR Subtotal<Discount OR Tax<0;
GO
SELECT i.Id,i.InvoiceNo,i.Status
FROM dbo.SalesInvoices i
WHERE i.Status=N'Posted'
AND NOT EXISTS(SELECT 1 FROM dbo.SalesCostSnapshots s WHERE s.InvoiceId=i.Id);
GO
