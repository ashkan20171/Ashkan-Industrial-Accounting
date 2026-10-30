from pathlib import Path
r=Path(__file__).resolve().parents[1]
s=(r/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text()
u=(r/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text()
q=(r/'database/017_BankImportFailureAudit.sql').read_text()
assert 'TryRecordImportFailure' in s
assert 'GetImportFailures' in s
assert 'new SqlCommand(@"INSERT dbo.BankImportFailureAudit' in s
assert 'AttemptedBy' in q and 'AttemptedAt' in q
assert 'Failed imports' in u
assert 'duplicate ? "DuplicateReference" : "DatabaseError"' in u
assert 'catch (Exception)' in s
print('7 stage64 source-contract checks passed')
