from pathlib import Path
r=Path(__file__).resolve().parents[1]
s=(r/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text(encoding='utf-8-sig')
u=(r/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8-sig')
d=(r/'database/018_BankImportAnalyticsIndexes.sql').read_text()
checks=[('paged SQL', 'OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY' in s),('deterministic sort','ORDER BY ImportedAt DESC, ImportId DESC' in s),('bounded page','pageSize > 200' in s),('parameterized','cmd.Parameters.Add("@offset"' in s),('UI paging','pageIndex++' in u and 'pageIndex--' in u),('SQL indexes','IX_BankImportHistory_AccountDateCover' in d)]
for name,ok in checks:
 print(('PASS' if ok else 'FAIL')+' '+name)
assert all(ok for _,ok in checks)
