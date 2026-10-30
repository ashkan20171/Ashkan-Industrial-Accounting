using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    public static class GrossProfitReportService
    {
        // Returned invoices are fully reversed and excluded from net sales.
        // InvoiceDate is the reporting basis, not the posting date.
        public static DataTable Read(DateTime fromInclusive, DateTime toInclusive)
        {
            if (toInclusive.Date < fromInclusive.Date) throw new ArgumentException("Invalid date range");
            const string sql = @"SELECT i.InvoiceNo, i.InvoiceDate, i.CurrencyCode,
                CAST(i.Subtotal-i.Discount AS decimal(18,2)) AS Revenue,
                CAST(i.Subtotal AS decimal(18,2)) AS Subtotal,
                CAST(i.Discount AS decimal(18,2)) AS Discount,
                CAST(i.Tax AS decimal(18,2)) AS Tax,
                CAST(COALESCE(c.Cost,0) AS decimal(18,2)) AS CostOfGoodsSold,
                CAST(i.Subtotal-i.Discount-COALESCE(c.Cost,0) AS decimal(18,2)) AS GrossProfit,
                CAST(CASE WHEN i.Subtotal-i.Discount=0 THEN 0 ELSE
                  (i.Subtotal-i.Discount-COALESCE(c.Cost,0))*100.0/
                  (i.Subtotal-i.Discount) END AS decimal(18,2)) AS MarginPercent
            FROM dbo.SalesInvoices i
            OUTER APPLY (SELECT SUM(s.TotalCost) AS Cost FROM dbo.SalesCostSnapshots s
                         WHERE s.InvoiceId=i.Id) c
            WHERE i.Status=N'Posted' AND i.InvoiceDate>=@from AND i.InvoiceDate<@until
              AND i.Subtotal>=i.Discount AND i.Tax>=0
              AND ABS(i.Total-(i.Subtotal-i.Discount+i.Tax))<=0.01
              AND EXISTS(SELECT 1 FROM dbo.SalesCostSnapshots s WHERE s.InvoiceId=i.Id)
            ORDER BY i.InvoiceDate DESC, i.Id DESC";
            using (var cn=Db.Open())
            using (var cmd=new SqlCommand(sql,cn))
            using (var adapter=new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@from",SqlDbType.DateTime2).Value=fromInclusive.Date;
                cmd.Parameters.Add("@until",SqlDbType.DateTime2).Value=toInclusive.Date.AddDays(1);
                var result=new DataTable();adapter.Fill(result);return result;
            }
        }
        public static int CountUncosted(DateTime fromInclusive,DateTime toInclusive)
        {
            using(var cn=Db.Open())
            using(var cmd=new SqlCommand(@"SELECT COUNT(*) FROM dbo.SalesInvoices i
                WHERE i.Status=N'Posted' AND i.InvoiceDate>=@from AND i.InvoiceDate<@until
                AND NOT EXISTS(SELECT 1 FROM dbo.SalesCostSnapshots s WHERE s.InvoiceId=i.Id)",cn))
            {
                cmd.Parameters.Add("@from",SqlDbType.DateTime2).Value=fromInclusive.Date;
                cmd.Parameters.Add("@until",SqlDbType.DateTime2).Value=toInclusive.Date.AddDays(1);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
