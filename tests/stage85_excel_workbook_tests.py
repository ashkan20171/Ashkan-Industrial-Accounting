from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
EXPORT=(ROOT/'AshkanIndustrialAccounting/Services/ReportExportService.cs').read_text(encoding='utf-8-sig')
FORM=(ROOT/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8-sig')
class Stage85ExcelWorkbookTests(unittest.TestCase):
 def test_multi_sheet_excel_xml_export_is_available(self):
  self.assertIn('SaveMultiSheetExcel(DataTable report',EXPORT)
  self.assertIn('urn:schemas-microsoft-com:office:spreadsheet',EXPORT)
  self.assertIn('WriteWorksheet(writer, ss, "All metrics", report)',EXPORT)
 def test_dashboard_creates_category_sheets(self):
  self.assertIn('foreach (string category in categories)',EXPORT)
  self.assertIn('view.ToTable()',EXPORT)
  self.assertIn('RowFilter = "Category =',EXPORT)
 def test_excel_strings_are_written_as_text_and_formula_like_values_escaped(self):
  self.assertIn('type == "String"',EXPORT)
  self.assertIn('text = "\'" + text',EXPORT)
 def test_sheet_names_are_sanitized_and_limited(self):
  self.assertIn('name.Length > 31',EXPORT)
  self.assertIn('Replace("/", "-")',EXPORT)
 def test_dashboard_exposes_multisheet_export_button(self):
  self.assertIn('Multi-sheet Excel',FORM)
  self.assertIn('SaveMultiSheetExcel(filtered.ToTable()',FORM)
 def test_existing_csv_and_print_are_preserved(self):
  self.assertIn('SaveCsv(grid,"BankAlertManagementDashboard")',FORM)
  self.assertIn('ReportPrintService.Print(grid,view.Text+',FORM)
if __name__=='__main__': unittest.main()
