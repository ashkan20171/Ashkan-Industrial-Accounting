from pathlib import Path
p=Path(__file__).resolve().parents[1]/"AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs"
s=p.read_text(encoding="utf-8-sig")
checks={
"kpi panel": "var kpiBar=new TableLayoutPanel" in s,
"four columns": "ColumnCount=4" in s,
"success KPI": 'kpiLabels[0].Text=' in s,
"failure KPI": 'kpiLabels[1].Text=' in s,
"success rate KPI": 'kpiLabels[2].Text=' in s,
"row count KPI": 'kpiLabels[3].Text=' in s,
"filtered chart": 'new DataView(data) { RowFilter="FailedImports > 0" }.ToTable()' in s,
"paint declaration order": s.index('DataTable data=null;',s.index('private void ShowImportAnalytics()')) < s.index('chart.Paint+=',s.index('private void ShowImportAnalytics()')),
"chart refresh": 'grid.DataSource=view;\n                    chart.Invalidate();' in s,
}
for k,v in checks.items():print(("PASS" if v else "FAIL"),k)
assert all(checks.values())
