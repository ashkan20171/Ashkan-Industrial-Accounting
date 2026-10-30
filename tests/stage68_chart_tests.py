from pathlib import Path
p=Path(__file__).resolve().parents[1]/"AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs"
s=p.read_text()
for expected in ["PaintImportAnalyticsChart", "chart.Invalidate();", "Math.Min(14,days.Count)", "SuccessfulImports", "FailedImports", "g.FillRectangle(successBrush", "g.FillRectangle(failureBrush", "dialog.Controls.Add(chart)"]:
    assert expected in s, expected
print("8 Stage68 source contract checks passed")
