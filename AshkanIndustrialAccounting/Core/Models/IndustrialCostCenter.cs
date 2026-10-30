namespace AshkanIndustrialAccounting.Core.Models
{
    public sealed class IndustrialCostCenter
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Budget { get; set; }
        public decimal ActualCost { get; set; }
        public decimal Variance => Budget - ActualCost;
    }
}
