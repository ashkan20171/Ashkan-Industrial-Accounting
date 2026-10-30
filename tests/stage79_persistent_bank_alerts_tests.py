from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
SERVICE=(ROOT/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text(encoding='utf-8-sig')
FORM=(ROOT/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8-sig')
SQL=(ROOT/'database/019_BankImportAlerts.sql').read_text(encoding='utf-8-sig')
class Stage79PersistentAlerts(unittest.TestCase):
 def test_migration_creates_alert_table(self): self.assertIn('CREATE TABLE dbo.BankImportAlerts',SQL)
 def test_scope_has_unique_constraint(self): self.assertIn('UX_BankImportAlerts_Scope',SQL)
 def test_alert_status_is_constrained(self):
  for x in ("N'New'","N'InProgress'","N'Resolved'"): self.assertIn(x,SQL)
 def test_save_requires_cashbank_permission(self): self.assertIn('public static int SaveBankAlert',SERVICE); self.assertIn('AuthorizationService.RequireActiveSession("cashbank")',SERVICE)
 def test_upsert_uses_serializable_transaction(self): self.assertIn('BeginTransaction(IsolationLevel.Serializable)',SERVICE)
 def test_alert_listing_is_parameterized(self): self.assertIn('public static DataTable GetBankAlerts',SERVICE); self.assertIn('cmd.Parameters.Add("@from"',SERVICE)
 def test_status_update_records_resolver(self): self.assertIn('ResolvedBy=CASE WHEN @status=N\'Resolved\' THEN @user',SERVICE)
 def test_ui_persists_and_loads_alerts(self): self.assertIn('SaveBankAlert(',FORM); self.assertIn('GetBankAlerts(',FORM)
 def test_ui_can_update_status(self): self.assertIn('UpdateBankAlertStatus(',FORM)
 def test_filtered_export_remains_available(self): self.assertIn('view.ToTable()',FORM); self.assertIn('bank-alert-register.csv',FORM)
if __name__=='__main__': unittest.main()
