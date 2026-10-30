using System;
using System.Linq;
using AshkanIndustrialAccounting.Core.Models;
using AshkanIndustrialAccounting.Infrastructure.Repositories;
namespace AshkanIndustrialAccounting.Services
{
    public sealed class AccountingService
    {
        private readonly JournalRepository _repo = new JournalRepository();
        public int Post(JournalEntry entry)
        {
            AuthorizationService.RequireActiveSession("journal");
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (entry.IsPosted) throw new InvalidOperationException("The journal entry is already posted.");
            if (string.IsNullOrWhiteSpace(entry.EntryNo)) throw new InvalidOperationException("Entry number is required.");
            if (entry.EntryDate == DateTime.MinValue) throw new InvalidOperationException("Entry date is required.");
            if (entry.Lines == null || entry.Lines.Count < 2) throw new InvalidOperationException("At least two journal lines are required.");
            if (entry.Lines.Any(x => x == null || x.AccountId <= 0 || x.Debit < 0 || x.Credit < 0 ||
                (x.Debit > 0 && x.Credit > 0) || (x.Debit == 0 && x.Credit == 0)))
                throw new InvalidOperationException("Each line must have a valid account and exactly one positive amount.");
            var debit = entry.Lines.Sum(x => x.Debit);
            var credit = entry.Lines.Sum(x => x.Credit);
            if (debit <= 0 || debit != credit) throw new InvalidOperationException("Journal entry is not balanced.");
            entry.IsPosted = true;
            try { return _repo.Save(entry); }
            catch { entry.IsPosted = false; throw; }
        }
    }
}
