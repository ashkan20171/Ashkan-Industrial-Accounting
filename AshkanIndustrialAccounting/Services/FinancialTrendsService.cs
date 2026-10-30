using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Monthly gross-profit trend for complete posted invoices only.</summary>
    public static class FinancialTrendsService
    {
        public static DataTable Read(DateTime from, DateTime to)
        {
            if (from.Date > to.Date) throw new ArgumentException("Invalid date range.");
            const string sql = @"WITH Eligible AS (
SELECT i.Id,i.InvoiceDate,i.CurrencyCode,
       i.Subtotal-i.Discount AS NetRevenue, c.Cost
FROM dbo.SalesInvoices i
CROSS APPLY (
 SELECT SUM(s.TotalCost) Cost FROM dbo.SalesCostSnapshots s
 WHERE s.InvoiceId=i.Id
) c
WHERE i.Status=N'Posted'
 AND i.InvoiceDate>=@from AND i.InvoiceDate<@until
 AND i.Subtotal>=i.Discount AND i.Tax>=0
 AND ABS(i.Total-(i.Subtotal-i.Discount+i.Tax))<=0.01
 AND c.Cost IS NOT NULL
 AND NOT EXISTS (
   SELECT 1 FROM dbo.SalesInvoiceLines l
   WHERE l.InvoiceId=i.Id AND NOT EXISTS (
     SELECT 1 FROM dbo.SalesCostSnapshots s
     WHERE s.InvoiceId=i.Id AND s.ProductId=l.ProductId
   )
 )
)
SELECT YEAR(InvoiceDate) AS [Year], MONTH(InvoiceDate) AS [Month],
 CurrencyCode, COUNT(*) AS InvoiceCount,
 CAST(SUM(NetRevenue) AS decimal(18,2)) AS NetRevenue,
 CAST(SUM(Cost) AS decimal(18,2)) AS CostOfGoodsSold,
 CAST(SUM(NetRevenue-Cost) AS decimal(18,2)) AS GrossProfit
FROM Eligible
GROUP BY YEAR(InvoiceDate),MONTH(InvoiceDate),CurrencyCode
ORDER BY [Year],[Month],CurrencyCode";
            using(var cn=Db.Open())
            using(var cmd=new SqlCommand(sql,cn))
            using(var da=new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@from",SqlDbType.DateTime2).Value=from.Date;
                cmd.Parameters.Add("@until",SqlDbType.DateTime2).Value=to.Date.AddDays(1);
                var table=new DataTable(); da.Fill(table); return table;
            }
        }
    }
}
