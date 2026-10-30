using System;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Pure financial invariants shared by posting and regression tests.</summary>
    public static class FinancialRules
    {
        public static decimal NetRevenue(decimal subtotal, decimal discount, decimal tax, decimal total)
        {
            if (subtotal < 0m || discount < 0m || discount > subtotal || tax < 0m || total <= 0m)
                throw new InvalidOperationException("Invoice amounts must be non-negative and valid.");
            decimal net = subtotal - discount;
            if (Math.Abs(total - (net + tax)) > 0.01m)
                throw new InvalidOperationException("Invoice totals do not reconcile.");
            return net;
        }

        public static decimal RemainingStock(decimal available, decimal requested)
        {
            if (available < 0m || requested <= 0m || available < requested)
                throw new InvalidOperationException("Insufficient or invalid stock.");
            return available - requested;
        }

        public static void ValidateBalanced(decimal debit, decimal credit, int lineCount)
        {
            if (lineCount < 2 || debit < 0m || credit < 0m || Math.Abs(debit-credit)>0.01m)
                throw new InvalidOperationException("Journal entry is not balanced.");
        }
    }
}
