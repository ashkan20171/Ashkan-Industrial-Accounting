import unittest
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]

class EmailNotificationTests(unittest.TestCase):
    def test_migration_adds_optional_user_email(self):
        sql=(ROOT/'database/023_EmailNotificationService.sql').read_text(encoding='utf-8')
        self.assertIn("COL_LENGTH(N'dbo.AppUsers',N'Email')", sql)
        self.assertIn('nvarchar(254) NULL', sql)
    def test_email_is_opt_in_by_default(self):
        sql=(ROOT/'database/023_EmailNotificationService.sql').read_text(encoding='utf-8')
        self.assertIn('EnableEmail bit NOT NULL', sql)
        self.assertIn('DEFAULT(0)', sql)
    def test_queue_is_idempotent_per_notification(self):
        sql=(ROOT/'database/023_EmailNotificationService.sql').read_text(encoding='utf-8')
        self.assertIn('UNIQUE(NotificationId)', sql)
        worker=(ROOT/'BankNotificationWorker/Program.cs').read_text(encoding='utf-8')
        self.assertIn('NOT EXISTS(SELECT 1 FROM dbo.EmailNotificationOutbox e WHERE e.NotificationId=n.Id)', worker)
    def test_bank_worker_respects_email_opt_in_and_address(self):
        worker=(ROOT/'BankNotificationWorker/Program.cs').read_text(encoding='utf-8')
        self.assertIn('ISNULL(p.EnableEmail,0)=1', worker)
        self.assertIn('NULLIF(LTRIM(RTRIM(u.Email)),N\'\') IS NOT NULL', worker)
    def test_email_worker_uses_independent_queue(self):
        worker=(ROOT/'EmailNotificationWorker/Program.cs').read_text(encoding='utf-8')
        self.assertIn('dbo.EmailNotificationOutbox', worker)
        self.assertIn('UPDLOCK,READPAST,ROWLOCK', worker)
    def test_email_worker_has_retry_and_dead_letter(self):
        worker=(ROOT/'EmailNotificationWorker/Program.cs').read_text(encoding='utf-8')
        self.assertIn("N'DeadLetter'", worker)
        self.assertIn('DATEADD(MINUTE,@delay,SYSUTCDATETIME())', worker)
        self.assertIn('MaxAttempts', worker)
    def test_smtp_configuration_is_externalized(self):
        root=ET.parse(ROOT/'EmailNotificationWorker/App.config').getroot()
        keys={x.attrib['key'] for x in root.findall('./appSettings/add')}
        self.assertTrue({'SmtpHost','SmtpPort','SmtpFrom','SmtpUser','SmtpPassword','SmtpEnableSsl'} <= keys)
    def test_worker_project_targets_net48(self):
        root=ET.parse(ROOT/'EmailNotificationWorker/EmailNotificationWorker.csproj').getroot()
        ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
        self.assertEqual(root.find('./m:PropertyGroup/m:TargetFrameworkVersion',ns).text,'v4.8')

if __name__ == '__main__': unittest.main()
