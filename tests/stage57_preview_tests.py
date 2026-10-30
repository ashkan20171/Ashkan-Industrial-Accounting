from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text()
assert 'grid.DataSource=table.DefaultView' in s
assert 'Show issues only' in s
assert 'table.DefaultView.RowFilter' in s
assert 'BankImportPreviewValidator.Validate(table, existing)' in s
assert 'confirm.Enabled=validated.Count>0 && invalid==0' in s
assert 'SourceRow,Date,Reference,Amount,Description,Status' in s
assert 'grid.DataError+=' in s
print('7 Stage 57 structural checks passed')
