from pathlib import Path
root=Path(__file__).resolve().parents[1]
service=(root/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text()
form=(root/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text()
sql=(root/'database/016_BankImportHistory.sql').read_text()
assert 'Guid.NewGuid()' in service
assert 'history.ExecuteNonQuery();' in service
assert 'ImportId)' in service
assert 'tx.Commit();' in service
assert 'GetImportHistory' in service and 'GetImportHistory' in form
assert 'Export history CSV' in form
assert 'FOREIGN KEY(ImportId)' in sql
assert 'IF COL_LENGTH' in sql
print('Stage62 structural checks: 8 passed')
