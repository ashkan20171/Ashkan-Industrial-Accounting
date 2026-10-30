from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]

def read(path): return (ROOT/path).read_text(encoding='utf-8-sig')

class NotificationCenterTests(unittest.TestCase):
    def test_inbox_service_is_registered_in_project(self):
        p=read('AshkanIndustrialAccounting/AshkanIndustrialAccounting.csproj')
        self.assertIn('Services\\NotificationInboxService.cs',p)
        self.assertIn('UI\\Forms\\NotificationCenterForm.cs',p)
    def test_inbox_queries_are_user_scoped(self):
        s=read('AshkanIndustrialAccounting/Services/NotificationInboxService.cs')
        self.assertIn('WHERE UserId=@user',s)
        self.assertIn('ResolveCurrentUserId',s)
        self.assertIn('WHERE Id=@id AND UserId=@user',s)
    def test_user_id_is_parameterized(self):
        s=read('AshkanIndustrialAccounting/Services/NotificationInboxService.cs')
        self.assertIn('SqlDbType.Int).Value = userId',s)
        self.assertNotIn('" + AuthorizationService.CurrentUserName',s)
    def test_mark_read_is_idempotent_and_scoped(self):
        s=read('AshkanIndustrialAccounting/Services/NotificationInboxService.cs')
        self.assertIn('ReadAtUtc IS NULL',s)
        self.assertIn('ReadAtUtc=SYSUTCDATETIME()',s)
    def test_center_supports_refresh_and_bulk_read(self):
        s=read('AshkanIndustrialAccounting/UI/Forms/NotificationCenterForm.cs')
        for token in ['GetInbox(', 'GetUnreadCount(', 'MarkAsRead(', 'MarkAllAsRead(', 'Unread only', 'فقط خوانده‌نشده‌ها']:
            self.assertIn(token,s)
    def test_navigation_and_permissions_registered(self):
        m=read('AshkanIndustrialAccounting/UI/Forms/MainForm.cs')
        a=read('AshkanIndustrialAccounting/Services/AuthorizationService.cs')
        self.assertIn('"notifications"',m)
        self.assertIn('new NotificationCenterForm()',m)
        self.assertIn('case "dashboard": case "reports": case "notifications": return true;',a)
    def test_bilingual_labels_exist(self):
        s=read('AshkanIndustrialAccounting/Services/LocalizationService.cs')
        self.assertIn('["notifications"]="Notifications"',s)
        self.assertIn('["notifications"]="مرکز اعلان‌ها"',s)
    def test_outbox_worker_remains_separate(self):
        self.assertTrue((ROOT/'BankNotificationWorker/Program.cs').exists())
        self.assertIn('BankAlertNotificationOutbox',read('BankNotificationWorker/Program.cs'))

if __name__=='__main__': unittest.main()
