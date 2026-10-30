import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

class NotificationNavigationTests(unittest.TestCase):
    def test_inbox_query_includes_related_alert_id(self):
        source = (ROOT / 'AshkanIndustrialAccounting/Services/NotificationInboxService.cs').read_text(encoding='utf-8')
        self.assertIn('SELECT Id,AlertId,Title,Message,SeverityCode,CreatedAtUtc,ReadAtUtc', source)
        self.assertIn('WHERE UserId=@user AND (@unread=0 OR ReadAtUtc IS NULL)', source)

    def test_alert_navigation_checks_cashbank_permission(self):
        source = (ROOT / 'AshkanIndustrialAccounting/UI/Forms/NotificationCenterForm.cs').read_text(encoding='utf-8')
        self.assertIn('AuthorizationService.CanAccess("cashbank")', source)
        self.assertIn('shell.NavigateToFeature("cashbank")', source)
        self.assertIn('NotificationInboxService.MarkAsRead(notificationId)', source)

    def test_notification_id_remains_user_scoped(self):
        source = (ROOT / 'AshkanIndustrialAccounting/Services/NotificationInboxService.cs').read_text(encoding='utf-8')
        self.assertIn('WHERE Id=@id AND UserId=@user AND ReadAtUtc IS NULL', source)

    def test_main_navigation_refreshes_badge_periodically(self):
        source = (ROOT / 'AshkanIndustrialAccounting/UI/Forms/MainForm.cs').read_text(encoding='utf-8')
        self.assertIn('_notificationTimer.Interval = 30000', source)
        self.assertIn('RefreshNotificationBadge()', source)
        self.assertIn('count > 0 ? "  (" + count + ")" : ""', source)

    def test_timer_is_disposed(self):
        source = (ROOT / 'AshkanIndustrialAccounting/UI/Forms/MainForm.cs').read_text(encoding='utf-8')
        self.assertIn('_notificationTimer.Stop(); _notificationTimer.Dispose();', source)

if __name__ == '__main__':
    unittest.main()
