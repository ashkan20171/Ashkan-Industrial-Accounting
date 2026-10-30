using System;
using System.Data;
using System.Data.SqlClient;

namespace AshkanIndustrialAccounting.Services
{
    internal static class SalesCostService
    {
        // Locks and records the cost at posting; returns reuse the original snapshot.
        internal static decimal CostForInvoice(SqlConnection cn, SqlTransaction tx, int invoiceId, bool reversal)
        {
            using (var cmd = new SqlCommand("SELECT COALESCE(SUM(TotalCost),0) FROM dbo.SalesCostSnapshots WITH (UPDLOCK,HOLDLOCK) WHERE InvoiceId=@id",cn,tx))
            {
                cmd.Parameters.Add("@id",SqlDbType.Int).Value=invoiceId;
                decimal result=Convert.ToDecimal(cmd.ExecuteScalar());
                if (result < 0) throw new InvalidOperationException("Negative cost");
                return result;
            }
        }
    }
}
