from pathlib import Path
root = Path(__file__).resolve().parents[1]
service = (root/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text()
form = (root/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text()
assert 'ImportStatementBatch(int accountId, IList<BankStatementCsvRow> rows)' in service
assert 'cn.BeginTransaction(IsolationLevel.Serializable)' in service
batch = service.split('public static int ImportStatementBatch',1)[1].split('public static int AddStatementLine',1)[0]
assert 'tx.Commit();' in batch
assert 'TransactionalAuditService.Record' in batch
assert 'AuthorizationService.RequireActiveSession("cashbank")' in batch
assert 'new HashSet<string>(StringComparer.OrdinalIgnoreCase)' in batch
assert 'ImportStatementBatch(SelectedAccount, ready)' in form
assert 'foreach(var row in ready)' not in form
print('Stage 50 source contract checks passed (not SQL integration tests).')
