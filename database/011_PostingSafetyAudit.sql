-- Stage 34: read-only diagnostic checks. Safe to run on production; no writes.
SET NOCOUNT ON;

-- Duplicate posting references must be investigated before creating unique constraints.
SELECT ReferenceNo, COUNT(*) AS DuplicateCount
FROM dbo.JournalEntries
WHERE ReferenceNo LIKE N'SALE-%'
GROUP BY ReferenceNo HAVING COUNT(*) > 1;

-- A posted invoice without its expected sales journal may indicate incomplete legacy data.
SELECT i.Id, i.InvoiceNo, i.Status
FROM dbo.SalesInvoices i
WHERE i.Status = N'Posted'
  AND NOT EXISTS (
    SELECT 1 FROM dbo.JournalEntries j
    WHERE j.EntryNo = N'SI-' + CONVERT(nvarchar(20),i.Id)
  );

-- Draft invoices that already have sales journals are not safe to post again.
SELECT i.Id, i.InvoiceNo
FROM dbo.SalesInvoices i
WHERE i.Status = N'Draft'
  AND EXISTS (
    SELECT 1 FROM dbo.JournalEntries j
    WHERE j.EntryNo = N'SI-' + CONVERT(nvarchar(20),i.Id)
       OR j.ReferenceNo = N'SALE-' + CONVERT(nvarchar(20),i.Id)
  );

-- Invalid financial totals in draft invoices.
SELECT Id, InvoiceNo, Subtotal, Discount, Tax, Total
FROM dbo.SalesInvoices
WHERE Status = N'Draft' AND
 (Subtotal < 0 OR Discount < 0 OR Discount > Subtotal OR Tax < 0
  OR Total <= 0 OR ABS(Total - (Subtotal - Discount + Tax)) > 0.01);
