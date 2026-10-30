from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
FORM=(ROOT/"AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs").read_text(encoding="utf-8-sig")
class Stage78BankAlertTests(unittest.TestCase):
 def test_alert_action_is_wired(self): self.assertIn('alerts.Click+=(s,e)=>Run(ShowBankAlerts)',FORM)
 def test_comparison_service_reused(self): self.assertIn('BankReconciliationService.GetAccountImportComparison(from.Value.Date,until.Value.Date.AddDays(1))',FORM)
 def test_critical_thresholds(self): self.assertIn('rate>=25m||bad>=5',FORM)
 def test_warning_thresholds(self): self.assertIn('rate>=10m||bad>=2',FORM)
 def test_information_alerts_exist(self): self.assertIn('"Information"',FORM)
 def test_actionable_recommendations(self): self.assertIn('Recommended action',FORM); self.assertIn('validate the source file',FORM)
 def test_severity_filter(self): self.assertIn("SeverityCode = 'Critical'",FORM); self.assertIn("SeverityCode = 'Warning'",FORM)
 def test_filtered_export(self): self.assertIn('var output=view.ToTable()',FORM)
 def test_utf8_bom_export(self): self.assertIn('new UTF8Encoding(true)',FORM)
 def test_date_range_limit(self): self.assertIn('max 366 days',FORM)
if __name__=='__main__': unittest.main()
