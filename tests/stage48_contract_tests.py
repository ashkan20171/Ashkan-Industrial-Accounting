from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text()
f=(root/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text()
assert 'public static void Unmatch(' in s
assert 'AuthorizationService.RequireActiveSession("cashbank")' in s
assert 'BankReconciliationReversed' in s and 'TransactionalAuditService.Record' in s
assert 'BeginTransaction(IsolationLevel.Serializable)' in s
assert 'MatchScore' in s and 'DateDifferenceDays' in s
assert 'private void UnmatchSelected()' in f and 'BankReconciliationService.Unmatch(' in f
print('Stage 48 source contract checks passed (6 assertions).')
