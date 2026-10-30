from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
SERVICE=(ROOT/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text(encoding='utf-8')
FORM=(ROOT/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8')
class Stage84AlertEfficiencyTests(unittest.TestCase):
 def test_resolution_rate_uses_resolved_records(self):
  self.assertIn("N'ResolutionPercent'",SERVICE)
  self.assertIn("StatusCode=N'Resolved' THEN 1 ELSE 0 END",SERVICE)
 def test_overdue_rate_excludes_resolved_and_missing_due_dates(self):
  self.assertIn("N'OverdueOpenPercent'",SERVICE)
  self.assertIn("StatusCode<>N'Resolved' AND DueAtUtc IS NOT NULL",SERVICE)
  self.assertIn('DueAtUtc<SYSUTCDATETIME()',SERVICE)
 def test_assignment_rate_uses_open_alerts(self):
  self.assertIn("N'AssignedOpenPercent'",SERVICE)
  self.assertIn('AssignedToUserId IS NOT NULL',SERVICE)
 def test_rates_are_scoped_to_selected_period(self):
  self.assertGreaterEqual(SERVICE.count('PeriodFrom>=@from AND PeriodFrom<@until'),10)
 def test_existing_dashboard_category_filter_supports_new_metrics(self):
  self.assertIn('filtered.RowFilter=',FORM)
  self.assertIn('Category =',FORM)
 def test_existing_csv_and_print_exports_remain_available(self):
  self.assertIn('ReportExportService.SaveCsv(grid,"BankAlertManagementDashboard")',FORM)
  self.assertIn('ReportPrintService.Print(grid,view.Text+',FORM)
if __name__=='__main__': unittest.main()
