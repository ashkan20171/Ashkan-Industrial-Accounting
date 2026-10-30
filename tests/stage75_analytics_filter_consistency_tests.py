from pathlib import Path
root=Path(__file__).resolve().parents[1]
form=(root/"AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs").read_text(encoding="utf-8-sig")
start=form.index("private void ShowImportAnalytics")
end=form.index("private static void PaintImportAnalyticsChart",start)
block=form[start:end]
checks={
 "shared filtered presentation action": "Action refreshPresentation" in block,
 "filter refreshes KPI and summary": "showOnlyIssues.CheckedChanged+=(sender,args)=>refreshPresentation()" in block and "AggregateImportAnalytics(visible)" in block and "AggregateImportAnalytics(priorView.ToTable())" in block,
 "filtered chart refresh": "chart.Invalidate()" in block and "RowFilter=\"FailedImports > 0\"" in block,
 "CSV exports filtered rows": "ExportAnalyticsCsv(exportView.ToTable(),save.FileName)" in block,
 "print preview button exists": "Print preview" in block or "پیش‌نمایش چاپ" in block,
 "print uses filtered grid": "ReportPrintService.Print(grid,dialog.Text" in block,
}
for name,ok in checks.items(): print(("PASS" if ok else "FAIL")+" - "+name)
assert all(checks.values())
