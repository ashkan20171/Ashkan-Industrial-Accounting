from pathlib import Path
root=Path(__file__).resolve().parents[1]
form=(root/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8-sig')
start=form.index('private void ShowImportAnalytics')
end=form.index('private sealed class ImportAnalyticsTotals',start)
block=form[start:end]
checks={
 'loads preceding equal-length period': 'int periodDays=(until.Value.Date-from.Value.Date).Days+1;' in block and 'from.Value.Date.AddDays(-periodDays)' in block and 'previousData=BankReconciliationService.GetImportAnalytics' in block,
 'comparison uses exclusive current start boundary': 'previousFrom,from.Value.Date' in block,
 'failure-only filter applied to both periods': block.count('RowFilter="FailedImports > 0"') >= 2,
 'success KPI shows period delta': 'FormatPeriodChange(current.Successes,prior.Successes,fa)' in block,
 'failure KPI shows period delta': 'FormatPeriodChange(current.Failures,prior.Failures,fa)' in block,
 'imported rows KPI shows period delta': 'FormatPeriodChange(current.Rows,prior.Rows,fa)' in block,
 'rate uses percentage-point difference': 'FormatPercentagePointChange(rate,priorRate)' in block,
 'incoming and outgoing compare against prior period': 'FormatPeriodChange(current.Incoming,prior.Incoming,fa)' in block and 'FormatPeriodChange(current.Outgoing,prior.Outgoing,fa)' in block,
 'shared aggregation helper includes monetary totals': 'totals.Incoming+=Convert.ToDecimal(row["TotalIncoming"])' in form and 'totals.Outgoing+=Convert.ToDecimal(row["TotalOutgoing"])' in form,
 'period comparison context disclosed': 'Compared with preceding equal-length period' in block,
}
for name,ok in checks.items(): print(('PASS' if ok else 'FAIL')+' - '+name)
assert all(checks.values())
