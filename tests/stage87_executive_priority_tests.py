from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
SERVICE=(ROOT/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text(encoding='utf-8-sig')
FORM=(ROOT/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8-sig')
class Stage87ExecutivePriorityTests(unittest.TestCase):
 def dashboard(self): return SERVICE.split('public static DataTable GetBankAlertDashboard(DateTime fromDate, DateTime untilDateExclusive, int?')[1].split('public static DataTable GetBankAlertAccounts()',1)[0]
 def test_critical_open_metric(self): self.assertIn("N'ExecutivePriority', N'CriticalOpen'",self.dashboard()); self.assertIn("SeverityCode=N'Critical'",self.dashboard())
 def test_critical_overdue_metric(self): self.assertIn("N'CriticalOverdue'",self.dashboard()); self.assertIn('DueAtUtc<SYSUTCDATETIME()',self.dashboard())
 def test_due_soon_metric_uses_24_hour_window(self):
  b=self.dashboard(); self.assertIn("N'DueWithin24Hours'",b); self.assertIn('DATEADD(HOUR,24,SYSUTCDATETIME())',b)
 def test_unassigned_critical_metric(self): self.assertIn("N'UnassignedCritical'",self.dashboard()); self.assertIn('AssignedToUserId IS NULL',self.dashboard())
 def test_all_metrics_use_filtered_population(self): self.assertGreaterEqual(self.dashboard().count('FROM FilteredAlerts'),15)
 def test_priority_category_is_selectable(self): self.assertIn('"ExecutivePriority"',FORM)
 def test_existing_filters_remain_parameterized(self):
  b=self.dashboard(); self.assertIn('@accountId IS NULL OR CashBankAccountId=@accountId',b); self.assertIn('@assigneeId IS NULL OR AssignedToUserId=@assigneeId',b)
if __name__=='__main__': unittest.main()
