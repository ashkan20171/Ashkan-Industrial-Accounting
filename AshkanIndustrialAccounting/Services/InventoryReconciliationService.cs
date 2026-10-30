using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    public static class InventoryReconciliationService
    {
        // Read-only audit: does not silently initialize or change opening balances.
        public static DataTable GetDifferences()
        {
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT p.Id AS ProductId,p.SKU,p.Name,
 p.StockQty AS PhysicalQuantity,v.Quantity AS ValuedQuantity,
 v.TotalValue AS InventoryValue,
 CASE WHEN v.ProductId IS NULL THEN N'Missing opening valuation'
      WHEN p.StockQty <> v.Quantity THEN N'Quantity mismatch'
      ELSE N'OK' END AS ReconciliationStatus
 FROM dbo.Products p LEFT JOIN dbo.InventoryValuations v ON v.ProductId=p.Id
 WHERE v.ProductId IS NULL OR p.StockQty<>v.Quantity
 ORDER BY p.SKU",cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                var table = new DataTable(); da.Fill(table); return table;
            }
        }
    }
}
