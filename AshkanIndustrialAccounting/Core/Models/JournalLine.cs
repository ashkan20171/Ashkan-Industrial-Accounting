namespace AshkanIndustrialAccounting.Core.Models
{
    public sealed class JournalLine
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public int? CostCenterId { get; set; }
    }
}
