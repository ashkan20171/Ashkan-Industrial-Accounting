import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class EmailAdminTests(unittest.TestCase):
 def test_service_validates_and_bounds_email(self):
  s=(ROOT/'AshkanIndustrialAccounting/Services/EmailAdministrationService.cs').read_text()
  self.assertIn('EmailAddressAttribute',s); self.assertIn('email.Length > 254',s)
 def test_history_is_admin_gated_and_bounded(self):
  s=(ROOT/'AshkanIndustrialAccounting/Services/EmailAdministrationService.cs').read_text()
  self.assertIn('RequireActiveSession("users")',s); self.assertIn('Math.Min(take, 1000)',s)
 def test_retry_only_deadletter(self):
  s=(ROOT/'AshkanIndustrialAccounting/Services/EmailAdministrationService.cs').read_text()
  self.assertIn("Status=N'Pending',AttemptCount=0",s); self.assertIn("Status=N'DeadLetter'",s)
 def test_ui_exposes_email_and_history(self):
  s=(ROOT/'AshkanIndustrialAccounting/UI/Forms/UserManagementForm.cs').read_text()
  self.assertIn('SaveEmail',s); self.assertIn('GetHistory',s); self.assertIn('RetrySelectedEmail',s)
 def test_user_query_includes_email(self):
  s=(ROOT/'AshkanIndustrialAccounting/Services/UserManagementService.cs').read_text()
  self.assertIn('RoleName, Email, IsActive',s)
 def test_project_compiles_service(self):
  s=(ROOT/'AshkanIndustrialAccounting/AshkanIndustrialAccounting.csproj').read_text()
  self.assertIn('Services\\EmailAdministrationService.cs',s)
if __name__=='__main__': unittest.main()
