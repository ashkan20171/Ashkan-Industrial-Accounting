from pathlib import Path
r=Path(__file__).resolve().parents[1]/'AshkanIndustrialAccounting'
s=(r/'Services/BankReconciliationService.cs').read_text(encoding='utf-8-sig')
f=(r/'UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8-sig')
checks=[('service', 'GetAccountImportComparison' in s),('group successes','GROUP BY CashBankAccountId) s' in s),('group failures','GROUP BY CashBankAccountId) f' in s),('bounded','TotalDays > 367' in s),('sql parameters','SqlDbType.DateTime2' in s),('UI', 'ShowAccountComparison' in f),('csv', 'CsvSafeCell' in f),('authorization','RequireActiveSession("cashbank")' in s)]
for name,ok in checks:
 print(('PASS' if ok else 'FAIL'),name)
assert all(ok for _,ok in checks)
