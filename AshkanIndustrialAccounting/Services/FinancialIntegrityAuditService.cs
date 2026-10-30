using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;
namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Read-only checks: never alters accounting records.</summary>
    public static class FinancialIntegrityAuditService
    {
        public static DataTable Run()
        {
            const string sql = @"SELECT N'UNBALANCED_JOURNAL' IssueCode, CAST(e.Id AS nvarchar(50)) RecordId,
 N'Posted journal not balanced or fewer than two lines' Detail
 FROM dbo.JournalEntries e LEFT JOIN dbo.JournalLines l ON l.JournalEntryId=e.Id
 WHERE e.IsPosted=1 GROUP BY e.Id
 HAVING COUNT(l.Id)<2 OR ABS(COALESCE(SUM(l.Debit),0)-COALESCE(SUM(l.Credit),0))>0.01
 UNION ALL
 SELECT N'INVOICE_TOTAL',CAST(i.Id AS nvarchar(50)),N'Invoice total is inconsistent'
 FROM dbo.SalesInvoices i WHERE i.Status=N'Posted' AND
 (i.Subtotal<i.Discount OR i.Tax<0 OR ABS(i.Total-(i.Subtotal-i.Discount+i.Tax))>0.01)
 UNION ALL
 SELECT N'MISSING_COST',CAST(i.Id AS nvarchar(50)),N'Posted invoice line has no cost snapshot'
 FROM dbo.SalesInvoices i WHERE i.Status=N'Posted' AND EXISTS
 (SELECT 1 FROM dbo.SalesInvoiceLines l WHERE l.InvoiceId=i.Id AND NOT EXISTS
 (SELECT 1 FROM dbo.SalesCostSnapshots s WHERE s.InvoiceId=l.InvoiceId AND s.ProductId=l.ProductId))
 UNION ALL
 SELECT N'COST_QUANTITY',CAST(i.Id AS nvarchar(50)),N'Cost snapshot quantity differs from invoice quantity'
 FROM dbo.SalesInvoices i WHERE i.Status=N'Posted' AND EXISTS
 (SELECT 1 FROM dbo.SalesCostSnapshots s WHERE s.InvoiceId=i.Id AND
 ABS(s.Quantity-(SELECT COALESCE(SUM(l.Qty),0) FROM dbo.SalesInvoiceLines l
 WHERE l.InvoiceId=s.InvoiceId AND l.ProductId=s.ProductId))>0.001)
 UNION ALL
 SELECT N'INVENTORY_MISMATCH',CAST(p.Id AS nvarchar(50)),N'Physical and valued inventory differ'
 FROM dbo.Products p LEFT JOIN dbo.InventoryValuations v ON v.ProductId=p.Id
 WHERE (v.ProductId IS NULL AND p.StockQty<>0) OR
 (v.ProductId IS NOT NULL AND ABS(p.StockQty-v.Quantity)>0.001)
 UNION ALL
 SELECT N'NEGATIVE_STOCK',CAST(p.Id AS nvarchar(50)),N'Negative physical stock'
 FROM dbo.Products p WHERE p.StockQty<0
 ORDER BY IssueCode,RecordId";
            using(var cn=Db.Open()) using(var cmd=new SqlCommand(sql,cn))
            using(var adapter=new SqlDataAdapter(cmd))
            { cmd.CommandTimeout=60; var result=new DataTable();adapter.Fill(result);return result; }
        }
    }
}
