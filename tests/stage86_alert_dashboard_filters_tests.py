from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
SERVICE=(ROOT/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text(encoding='utf-8-sig')
FORM=(ROOT/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8-sig')
class Stage86AlertDashboardFilterTests(unittest.TestCase):
 def body(self): return SERVICE.split('public static DataTable GetBankAlertDashboard(DateTime fromDate, DateTime untilDateExclusive, int?')[1].split('public static DataTable GetBankAlertAccounts()',1)[0]
 def test_legacy_api_remains_compatible(self): self.assertIn('GetBankAlertDashboard(fromDate, untilDateExclusive, null, null)',SERVICE)
 def test_common_filtered_population(self): self.assertIn('WITH FilteredAlerts AS',self.body()); self.assertIn('SELECT * FROM dbo.BankImportAlerts',self.body())
 def test_account_and_assignee_filters_are_parameterized(self):
  b=self.body(); self.assertIn('@accountId IS NULL OR CashBankAccountId=@accountId',b); self.assertIn('@assigneeId IS NULL OR AssignedToUserId=@assigneeId',b)
  self.assertIn('cmd.Parameters.Add("@accountId"',b); self.assertIn('cmd.Parameters.Add("@assigneeId"',b)
 def test_every_metric_uses_filtered_cte(self):
  b=self.body(); self.assertIn('SELECT * FROM dbo.BankImportAlerts',b); self.assertGreaterEqual(b.count('FROM FilteredAlerts'),10)
 def test_account_and_assignee_selectors_exist(self): self.assertIn('GetBankAlertAccounts()',SERVICE); self.assertIn('GetBankAlertAssignees()',FORM)
 def test_filters_refresh_report_and_exports_filtered_rows(self):
  self.assertIn('GetBankAlertDashboard(fromDate,untilDateExclusive,accountId,assigneeId)',FORM)
  self.assertIn('SaveMultiSheetExcel(filtered.ToTable()',FORM); self.assertIn('SaveCsv(grid,\"BankAlertManagementDashboard\")',FORM)
if __name__=='__main__': unittest.main()
