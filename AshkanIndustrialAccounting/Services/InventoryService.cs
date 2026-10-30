using System;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    public static class InventoryService
    {
        // One transaction locks the product row, updates stock and records an immutable movement.
        // OperationId prevents duplicate stock movements on retry.
        public static decimal Record(int productId, string movementType, decimal quantity, string reference, string notes, string user, Guid operationId)
        {
            return RecordValued(productId, movementType, quantity, reference, notes, user, operationId, null);
        }

        // For inbound receipts or positive stock adjustments, callers must supply a documented unit cost.
        public static decimal RecordValued(int productId, string movementType, decimal quantity, string reference,
            string notes, string user, Guid operationId, decimal? inboundUnitCost)
        {
            AuthorizationService.RequireActiveSession("products");
            if (!String.Equals(user, AuthorizationService.CurrentUserName, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Inventory operator must match the active session.");
            if (productId <= 0 || quantity < 0 || quantity > 999999999m) throw new ArgumentOutOfRangeException("quantity");
            if (movementType != "IN" && movementType != "OUT" && movementType != "ADJUST") throw new ArgumentException("Invalid movement type");
            if (movementType != "ADJUST" && quantity == 0) throw new ArgumentException("Quantity must be positive");
            if (string.IsNullOrWhiteSpace(user)) throw new ArgumentException("User is required");
            if ((reference ?? "").Length > 80 || (notes ?? "").Length > 500 || user.Length > 100) throw new ArgumentException("Field too long");
            using (var cn = Db.Open())
            using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    using (var duplicate = new SqlCommand("SELECT StockAfter FROM dbo.InventoryMovements WHERE OperationId=@op", cn, tx))
                    {
                        duplicate.Parameters.Add("@op", SqlDbType.UniqueIdentifier).Value = operationId;
                        var existing = duplicate.ExecuteScalar();
                        if (existing != null) { tx.Commit(); return Convert.ToDecimal(existing); }
                    }
                    decimal before;
                    using (var select = new SqlCommand("SELECT StockQty FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE Id=@id AND IsActive=1", cn, tx))
                    {
                        select.Parameters.Add("@id", SqlDbType.Int).Value = productId;
                        var value = select.ExecuteScalar();
                        if (value == null) throw new InvalidOperationException("Active product not found");
                        before = Convert.ToDecimal(value);
                    }
                    decimal delta = movementType == "IN" ? quantity : movementType == "OUT" ? -quantity : quantity - before;
                    decimal after = before + delta;
                    if (after < 0) throw new InvalidOperationException("Insufficient stock");
                    if (delta == 0) throw new InvalidOperationException("No inventory change");
                    // Fail closed if physical stock and valuation opening balances differ.
                    decimal valuedQty;
                    using (var check = new SqlCommand(
                        "SELECT Quantity FROM dbo.InventoryValuations WITH(UPDLOCK,HOLDLOCK) WHERE ProductId=@id", cn, tx))
                    {
                        check.Parameters.Add("@id", SqlDbType.Int).Value = productId;
                        var value = check.ExecuteScalar();
                        if (value == null) throw new InvalidOperationException("Missing reconciled opening valuation");
                        valuedQty = Convert.ToDecimal(value);
                    }
                    if (valuedQty != before)
                        throw new InvalidOperationException("Physical and valued stock do not reconcile");
                    if (delta > 0m && (!inboundUnitCost.HasValue || inboundUnitCost.Value <= 0m))
                        throw new InvalidOperationException("Inbound unit cost is required for valuation");
                    decimal valuationChange = WeightedAverageValuationService.Apply(cn, tx, operationId, productId,
                        delta > 0m ? "RECEIPT" : "ISSUE", Math.Abs(delta),
                        inboundUnitCost.GetValueOrDefault());
                    InventoryJournalService.Record(cn, tx, operationId, movementType, valuationChange);
                    using (var update = new SqlCommand("UPDATE dbo.Products SET StockQty=@after WHERE Id=@id", cn, tx))
                    {
                        AddDecimal(update, "@after", after);
                        update.Parameters.Add("@id", SqlDbType.Int).Value = productId;
                        if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Product update failed");
                    }
                    using (var insert = new SqlCommand(@"INSERT dbo.InventoryMovements(OperationId,ProductId,MovementType,QuantityDelta,StockBefore,StockAfter,ReferenceNo,Notes,PerformedBy) VALUES(@op,@id,@type,@delta,@before,@after,@ref,@notes,@user)", cn, tx))
                    {
                        insert.Parameters.Add("@op", SqlDbType.UniqueIdentifier).Value = operationId;
                        insert.Parameters.Add("@id", SqlDbType.Int).Value = productId;
                        insert.Parameters.Add("@type", SqlDbType.VarChar, 10).Value = movementType;
                        AddDecimal(insert, "@delta", delta); AddDecimal(insert, "@before", before); AddDecimal(insert, "@after", after);
                        insert.Parameters.Add("@ref", SqlDbType.NVarChar, 80).Value = (object)reference ?? DBNull.Value;
                        insert.Parameters.Add("@notes", SqlDbType.NVarChar, 500).Value = (object)notes ?? DBNull.Value;
                        insert.Parameters.Add("@user", SqlDbType.NVarChar, 100).Value = user;
                        insert.ExecuteNonQuery();
                    }
                    TransactionalAuditService.Record(cn, tx, user, "InventoryMovement", "Product",
                        productId.ToString(), String.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "OperationId={0}; Type={1}; Before={2}; Delta={3}; After={4}; Reference={5}",
                        operationId, movementType, before, delta, after, reference ?? ""));
                    tx.Commit(); return after;
                }
                catch { tx.Rollback(); throw; }
            }
        }
        private static void AddDecimal(SqlCommand cmd, string key, decimal value)
        {
            var p = cmd.Parameters.Add(key, SqlDbType.Decimal); p.Precision = 18; p.Scale = 3; p.Value = value;
        }
        public static DataTable History(int productId)
        {
            AuthorizationService.RequireActiveSession("products");
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT CreatedAtUtc,MovementType,QuantityDelta,StockBefore,StockAfter,ReferenceNo,PerformedBy,Notes FROM dbo.InventoryMovements WHERE ProductId=@id ORDER BY Id DESC", cn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = productId;
                var result = new DataTable(); adapter.Fill(result); return result;
            }
        }
    }
}
