from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text()
f=(root/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text()
def test_analytics():
    assert 'GetImportAnalytics(int accountId, DateTime fromUtc, DateTime untilUtcExclusive)' in s
    assert 'CashBankAccountId=@account' in s
    assert 'ImportedAt>=@from AND ImportedAt<@until' in s
    assert 'AttemptedAt>=@from AND AttemptedAt<@until' in s
    assert 'UNION ALL' in s and 'GROUP BY x.DayUtc' in s
    assert 'SqlDbType.DateTime2' in s
    assert 'AuthorizationService.RequireActiveSession("cashbank")' in s
    assert 'ShowImportAnalytics' in f and 'Success rate:' in f
    assert 'ExportAnalyticsCsv' in f and 'new UTF8Encoding(true)' in f
    assert 'until.Value.Date.AddDays(1)' in f
if __name__=='__main__':
    test_analytics()
    print('10 analytics contract checks passed')
