using System;
using System.Collections.Generic;
namespace AshkanIndustrialAccounting.Core.Models
{
    public sealed class JournalEntry
    {
        public int Id { get; set; }
        public string EntryNo { get; set; }
        public DateTime EntryDate { get; set; }
        public string Description { get; set; }
        public string ReferenceNo { get; set; }
        public bool IsPosted { get; set; }
        public List<JournalLine> Lines { get; } = new List<JournalLine>();
    }
}
