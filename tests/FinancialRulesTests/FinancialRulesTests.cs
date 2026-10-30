using System;
using AshkanIndustrialAccounting.Services;

internal static class FinancialRulesTests
{
    private static int count;
    private static void Equal(decimal expected, decimal actual)
    {
        count++;
        if (expected != actual) throw new Exception("Expected "+expected+", got "+actual);
    }
    private static void Throws(Action action)
    {
        count++;
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new Exception("Expected InvalidOperationException");
    }
    private static int Main()
    {
        try
        {
            Equal(90m,FinancialRules.NetRevenue(100m,10m,9m,99m));
            Equal(0m,FinancialRules.NetRevenue(100m,100m,5m,5m));
            Throws(()=>FinancialRules.NetRevenue(100m,110m,0m,-10m));
            Throws(()=>FinancialRules.NetRevenue(100m,10m,9m,100m));
            Throws(()=>FinancialRules.NetRevenue(100m,-1m,0m,101m));
            Equal(7.5m,FinancialRules.RemainingStock(10m,2.5m));
            Equal(0m,FinancialRules.RemainingStock(3m,3m));
            Throws(()=>FinancialRules.RemainingStock(3m,4m));
            Throws(()=>FinancialRules.RemainingStock(3m,0m));
            FinancialRules.ValidateBalanced(99m,99m,3);count++;
            Throws(()=>FinancialRules.ValidateBalanced(99m,98m,3));
            Throws(()=>FinancialRules.ValidateBalanced(99m,99m,1));
            Console.WriteLine("PASS: "+count+" financial rules checks.");
            return 0;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine("FAIL: "+ex);
            return 1;
        }
    }
}
