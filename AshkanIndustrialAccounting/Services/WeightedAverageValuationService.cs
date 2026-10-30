using System;
using System.Data;
using System.Data.SqlClient;

namespace AshkanIndustrialAccounting.Services
{
    // Foundation for moving weighted-average costing. Must be called in the
    // SAME transaction as the corresponding physical inventory movement.
    internal static class WeightedAverageValuationService
    {
        internal static decimal Apply(SqlConnection cn, SqlTransaction tx, Guid operationId,
            int productId, string eventType, decimal quantity, decimal receiptUnitCost)
        {
            if (cn == null || tx == null) throw new ArgumentNullException("transaction");
            if (productId <= 0 || quantity <= 0m || quantity > 999999999m)
                throw new ArgumentOutOfRangeException("quantity");
            if (eventType != "RECEIPT" && eventType != "ISSUE")
                throw new ArgumentException("Unsupported valuation event");
            if (receiptUnitCost < 0m) throw new ArgumentOutOfRangeException("receiptUnitCost");
            using (var duplicate = new SqlCommand(
                "SELECT ValueDelta FROM dbo.InventoryValuationEvents WHERE OperationId=@op",cn,tx))
            {
                duplicate.Parameters.Add("@op",SqlDbType.UniqueIdentifier).Value=operationId;
                var prior=duplicate.ExecuteScalar();
                if (prior != null) return Convert.ToDecimal(prior);
            }
            decimal oldQty, oldValue;
            using (var read = new SqlCommand(
                "SELECT Quantity,TotalValue FROM dbo.InventoryValuations WITH(UPDLOCK,HOLDLOCK) WHERE ProductId=@id",cn,tx))
            {
                read.Parameters.Add("@id",SqlDbType.Int).Value=productId;
                using (var r=read.ExecuteReader())
                {
                    if (!r.Read()) throw new InvalidOperationException("Initialize and reconcile opening valuation first");
                    oldQty=r.GetDecimal(0); oldValue=r.GetDecimal(1);
                }
            }
            if (eventType == "ISSUE" && quantity > oldQty)
                throw new InvalidOperationException("Insufficient valued stock");
            decimal deltaQty=eventType=="RECEIPT" ? quantity : -quantity;
            decimal deltaValue=eventType=="RECEIPT"
                ? Math.Round(quantity*receiptUnitCost,2,MidpointRounding.AwayFromZero)
                : (quantity==oldQty ? -oldValue :
                   -Math.Round(oldValue/oldQty*quantity,2,MidpointRounding.AwayFromZero));
            decimal newQty=oldQty+deltaQty;
            decimal newValue=oldValue+deltaValue;
            if (newValue < 0) throw new InvalidOperationException("Negative inventory valuation");
            using (var cmd=new SqlCommand(
                "UPDATE dbo.InventoryValuations SET Quantity=@q,TotalValue=@v WHERE ProductId=@id",cn,tx))
            {
                cmd.Parameters.Add("@id",SqlDbType.Int).Value=productId;
                Decimal(cmd,"@q",newQty,3); Decimal(cmd,"@v",newValue,2);
                if(cmd.ExecuteNonQuery()!=1) throw new InvalidOperationException("Valuation update failed");
            }
            using (var cmd=new SqlCommand(@"INSERT dbo.InventoryValuationEvents
(OperationId,ProductId,EventType,QuantityDelta,ValueDelta,QuantityAfter,ValueAfter)
VALUES(@op,@id,@type,@dq,@dv,@q,@v)",cn,tx))
            {
                cmd.Parameters.Add("@op",SqlDbType.UniqueIdentifier).Value=operationId;
                cmd.Parameters.Add("@id",SqlDbType.Int).Value=productId;
                cmd.Parameters.Add("@type",SqlDbType.VarChar,12).Value=eventType;
                Decimal(cmd,"@dq",deltaQty,3); Decimal(cmd,"@dv",deltaValue,2);
                Decimal(cmd,"@q",newQty,3); Decimal(cmd,"@v",newValue,2);
                cmd.ExecuteNonQuery();
            }
            return deltaValue;
        }
        private static void Decimal(SqlCommand cmd,string key,decimal value,byte scale)
        {
            var p=cmd.Parameters.Add(key,SqlDbType.Decimal);
            p.Precision=18; p.Scale=scale; p.Value=value;
        }
    }
}
