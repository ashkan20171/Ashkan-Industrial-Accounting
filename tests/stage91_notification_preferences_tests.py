import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SERVICE = (ROOT / 'AshkanIndustrialAccounting/Services/NotificationInboxService.cs').read_text(encoding='utf-8-sig')
FORM = (ROOT / 'AshkanIndustrialAccounting/UI/Forms/NotificationCenterForm.cs').read_text(encoding='utf-8-sig')
WORKER = (ROOT / 'BankNotificationWorker/Program.cs').read_text(encoding='utf-8-sig')
MIGRATION = (ROOT / 'database/022_UserNotificationPreferences.sql').read_text(encoding='utf-8-sig')

class NotificationPreferenceTests(unittest.TestCase):
    def test_user_scoped_preferences(self):
        self.assertIn('ResolveCurrentUserId(cn)', SERVICE)
        self.assertIn('WHERE UserId=@user', SERVICE)

    def test_default_preferences_enable_delivery(self):
        self.assertIn('EnableInApp { get; set; } = true', SERVICE)
        self.assertIn('DEFAULT 1', MIGRATION)

    def test_all_supported_severities_have_preferences(self):
        for key in ('NotifyCritical', 'NotifyHigh', 'NotifyMedium', 'NotifyLow'):
            self.assertIn(key, SERVICE)
            self.assertIn(key, MIGRATION)

    def test_preference_upsert_is_parameterized(self):
        self.assertIn('IF @@ROWCOUNT=0 INSERT', SERVICE)
        self.assertIn('cmd.Parameters.Add("@critical"', SERVICE)

    def test_ui_exposes_preferences_dialog(self):
        self.assertIn('EditPreferences()', FORM)
        self.assertIn('Save preferences', FORM)

    def test_worker_checks_in_app_switch(self):
        self.assertIn('p.EnableInApp=0', WORKER)
        self.assertIn('if (!enabled)', WORKER)

    def test_worker_maps_alert_severities(self):
        self.assertIn("UPPER(a.SeverityCode)='CRITICAL'", WORKER)
        self.assertIn("UPPER(a.SeverityCode)='WARNING'", WORKER)
        self.assertIn("UPPER(a.SeverityCode)='INFORMATION'", WORKER)

    def test_disabled_notification_is_acknowledged_without_inbox_insert(self):
        branch = WORKER.split('if (!enabled)', 1)[1].split('using (var add', 1)[0]
        self.assertIn('ProcessedAtUtc=SYSUTCDATETIME()', branch)
        self.assertIn('tx.Commit()', branch)
        self.assertIn('continue;', branch)

if __name__ == '__main__':
    unittest.main()
