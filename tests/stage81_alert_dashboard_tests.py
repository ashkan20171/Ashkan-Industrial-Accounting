from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
SERVICE=(ROOT/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text(encoding='utf-8-sig')
FORM=(ROOT/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8-sig')
class Stage81AlertDashboard(unittest.TestCase):
 def test_dashboard_service_exists_and_requires_permission(self):
  body=SERVICE.split('public static DataTable GetBankAlertDashboard',1)[1].split('public static DataTable GetBankAlertAssignees',1)[0]
  self.assertIn('RequireActiveSession("cashbank")',body)
 def test_dashboard_date_range_is_bounded(self):
  body=SERVICE.split('public static DataTable GetBankAlertDashboard',1)[1].split('public static DataTable GetBankAlertAssignees',1)[0]
  self.assertIn('TotalDays > 367',body)
 def test_status_severity_and_priority_aggregations(self):
  for field in ('StatusCode','SeverityCode','PriorityCode'): self.assertIn('GROUP BY '+field,SERVICE)
 def test_unassigned_and_assigned_workload_is_counted(self):
  self.assertIn("N'Unassigned'",SERVICE); self.assertIn("N'Assigned'",SERVICE)
 def test_sla_buckets_are_reported(self):
  for bucket in ('NoDueDate','Overdue','OnTime'): self.assertIn("N'"+bucket+"'",SERVICE)
 def test_open_and_resolved_totals_are_reported(self):
  self.assertIn("N'Open'",SERVICE); self.assertIn("N'Resolved'",SERVICE)
 def test_dashboard_uses_parameterized_dates(self):
  body=SERVICE.split('public static DataTable GetBankAlertDashboard',1)[1].split('public static DataTable GetBankAlertAssignees',1)[0]
  self.assertIn('cmd.Parameters.Add("@from"',body); self.assertIn('cmd.Parameters.Add("@until"',body)
 def test_ui_exposes_management_dashboard(self):
  self.assertIn('GetBankAlertDashboard',FORM); self.assertIn('Management dashboard',FORM)
if __name__=='__main__': unittest.main()
