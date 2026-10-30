from pathlib import Path
root=Path(__file__).resolve().parents[1]/"AshkanIndustrialAccounting"
svc=(root/"Services/BankReconciliationService.cs").read_text()
form=(root/"UI/Forms/BankReconciliationForm.cs").read_text()
assert 'FindExistingReferences' in svc
assert 'AuthorizationService.RequireActiveSession("cashbank")' in svc
assert 'cmd.Parameters.Add(name, SqlDbType.NVarChar, 100)' in svc
assert 'new HashSet<string>(StringComparer.OrdinalIgnoreCase)' in form
assert 'Statement import preview' in form
assert 'preview.ShowDialog(this)!=DialogResult.OK' in form
assert 'if (!existing.Contains(row.Reference.Trim())' in form
print("7 structural import checks passed")
