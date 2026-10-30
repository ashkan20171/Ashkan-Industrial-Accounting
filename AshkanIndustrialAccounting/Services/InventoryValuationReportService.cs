using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    public static class InventoryValuationReportService
    {
        public static DataTable Read(bool differencesOnly)
        {
            const string sql = @"SELECT p.SKU,p.Name,p.StockQty AS PhysicalQuantity,
 v.Quantity AS ValuedQuantity,v.TotalValue AS InventoryValue,
 CASE WHEN v.ProductId IS NULL THEN N'Missing opening valuation'
 WHEN p.StockQty <> v.Quantity THEN N'Quantity mismatch' ELSE N'OK' END AS ReconciliationStatus
 FROM dbo.Products p LEFT JOIN dbo.InventoryValuations v ON p.Id=v.ProductId
 WHERE (@only=0 OR v.ProductId IS NULL OR p.StockQty<>v.Quantity)
 ORDER BY p.SKU";
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(sql, cn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@only", SqlDbType.Bit).Value = differencesOnly;
                var result = new DataTable(); adapter.Fill(result); return result;
            }
        }
    }
}
