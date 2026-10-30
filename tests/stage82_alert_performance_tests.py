from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
SERVICE=(ROOT/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text(encoding='utf-8-sig')
class Stage82AlertPerformance(unittest.TestCase):
 def dashboard(self):
  return SERVICE.split('public static DataTable GetBankAlertDashboard',1)[1].split('public static DataTable GetBankAlertAssignees',1)[0]
 def test_monthly_alert_trend_is_aggregated(self):
  self.assertIn("N'MonthlyTrend'",self.dashboard()); self.assertIn('GROUP BY CONVERT(nvarchar(7), PeriodFrom, 126)',self.dashboard())
 def test_open_workload_is_grouped_by_assignee(self):
  self.assertIn("N'AssigneeWorkload'",self.dashboard()); self.assertIn('GROUP BY COALESCE(u.DisplayName,N\'Unassigned\')',self.dashboard())
 def test_average_resolution_time_is_reported(self):
  self.assertIn("N'AverageResolutionMinutes'",self.dashboard()); self.assertIn('DATEDIFF_BIG(MINUTE',self.dashboard())
 def test_invalid_resolution_intervals_are_excluded(self):
  self.assertIn('ResolvedAt>=FirstDetectedAt',self.dashboard()); self.assertIn('x.ResolvedAt>=x.FirstDetectedAt',self.dashboard())
 def test_performance_report_keeps_date_parameters_and_permission(self):
  b=self.dashboard(); self.assertIn('RequireActiveSession("cashbank")',b); self.assertIn('cmd.Parameters.Add("@from"',b); self.assertIn('cmd.Parameters.Add("@until"',b)
if __name__=='__main__': unittest.main()
