from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
FORM=(ROOT/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8')
class Stage83DashboardTests(unittest.TestCase):
 def test_dashboard_uses_persisted_service_data(self): self.assertIn('BankReconciliationService.GetBankAlertDashboard(fromDate,untilDateExclusive)',FORM)
 def test_visual_chart_is_custom_gdi(self): self.assertIn('PaintBankAlertDashboardChart',FORM); self.assertIn('graphics.FillRectangle(bar',FORM)
 def test_category_filter_is_applied_to_grid(self): self.assertIn('filtered.RowFilter=',FORM); self.assertIn('Category =',FORM)
 def test_chart_reacts_to_filter(self): self.assertIn('chart.Invalidate()',FORM)
 def test_csv_export_uses_safe_shared_exporter(self): self.assertIn('ReportExportService.SaveCsv(grid,"BankAlertManagementDashboard")',FORM)
 def test_print_uses_shared_pagination_service(self): self.assertIn('ReportPrintService.Print(grid,view.Text+',FORM)
 def test_all_dashboard_entry_points_share_view(self): self.assertEqual(FORM.count('ShowBankAlertDashboard(from.Value.Date,until.Value.Date.AddDays(1),dialog)'),4)
 def test_dashboard_keeps_persian_and_english_labels(self): self.assertIn('داشبورد مدیریتی هشدارهای بانکی',FORM); self.assertIn('Bank alerts management dashboard',FORM)
if __name__=='__main__': unittest.main()
