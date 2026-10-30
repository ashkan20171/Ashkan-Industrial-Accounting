using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Read-only, currency-aware operational KPIs and accounting alerts.</summary>
    public static class ManagementKpiService
    {
        public static DataTable SalesByCurrency(DateTime from, DateTime to)
        {
            const string sql=@"SELECT CurrencyCode,COUNT(*) InvoiceCount,
CAST(SUM(Subtotal-Discount) AS decimal(18,2)) NetSales
FROM dbo.SalesInvoices
WHERE Status=N'Posted' AND InvoiceDate>=@from AND InvoiceDate<@until
 AND Subtotal>=Discount AND Tax>=0
 AND ABS(Total-(Subtotal-Discount+Tax))<=0.01
GROUP BY CurrencyCode ORDER BY CurrencyCode";
            using(var cn=Db.Open())
            using(var cmd=new SqlCommand(sql,cn))
            using(var da=new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@from",SqlDbType.DateTime2).Value=from.Date;
                cmd.Parameters.Add("@until",SqlDbType.DateTime2).Value=to.Date.AddDays(1);
                var data=new DataTable();da.Fill(data);return data;
            }
        }
        public static DataTable InventorySummary()
        {
            const string sql=@"SELECT
COUNT(*) ProductCount,
SUM(CASE WHEN p.StockQty<0 THEN 1 ELSE 0 END) NegativeStockProducts,
SUM(CASE WHEN v.ProductId IS NULL OR p.StockQty<>v.Quantity THEN 1 ELSE 0 END) ReconciliationAlerts,
SUM(CASE WHEN v.ProductId IS NOT NULL THEN v.TotalValue ELSE 0 END) TotalInventoryValue
FROM dbo.Products p
LEFT JOIN dbo.InventoryValuations v ON v.ProductId=p.Id";
            using(var cn=Db.Open())
            using(var cmd=new SqlCommand(sql,cn))
            using(var da=new SqlDataAdapter(cmd))
            {
                var data=new DataTable();da.Fill(data);return data;
            }
        }
        public static int UnbalancedPostedEntries()
        {
            const string sql=@"SELECT COUNT(*) FROM (
 SELECT e.Id
 FROM dbo.JournalEntries e LEFT JOIN dbo.JournalLines l ON l.JournalEntryId=e.Id
 WHERE e.IsPosted=1
 GROUP BY e.Id
 HAVING ABS(COALESCE(SUM(l.Debit),0)-COALESCE(SUM(l.Credit),0))>0.01
 OR COUNT(l.Id)<2
) x";
            using(var cn=Db.Open())
            using(var cmd=new SqlCommand(sql,cn))
                return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
