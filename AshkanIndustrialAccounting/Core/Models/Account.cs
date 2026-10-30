namespace AshkanIndustrialAccounting.Core.Models
{
    public sealed class Account
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string NameEn { get; set; }
        public string NameFa { get; set; }
        public string Type { get; set; }
        public bool IsActive { get; set; }
        public override string ToString() => Code + " - " + NameEn;
    }
}
