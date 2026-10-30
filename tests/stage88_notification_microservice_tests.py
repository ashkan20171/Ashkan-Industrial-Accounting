from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]

class NotificationMicroserviceTests(unittest.TestCase):
    def test_outbox_schema_is_idempotent_and_deduplicated(self):
        sql = (ROOT/'database/021_BankAlertNotifications.sql').read_text(encoding='utf-8')
        self.assertIn("IF OBJECT_ID(N'dbo.BankAlertNotificationOutbox',N'U') IS NULL", sql)
        self.assertIn('UQ_BankAlertNotificationOutbox_EventKey UNIQUE(EventKey)', sql)
        self.assertIn('UQ_UserNotifications_OutboxUser UNIQUE(OutboxId,UserId)', sql)

    def test_notification_delivery_has_atomic_outbox_and_inbox(self):
        worker = (ROOT/'BankNotificationWorker/Program.cs').read_text(encoding='utf-8')
        self.assertIn('BeginTransaction(IsolationLevel.ReadCommitted)', worker)
        self.assertIn('UPDLOCK,READPAST,ROWLOCK', worker)
        self.assertIn('IF NOT EXISTS(SELECT 1 FROM dbo.UserNotifications', worker)
        self.assertIn('ProcessedAtUtc=SYSUTCDATETIME()', worker)

    def test_worker_is_separate_net48_executable(self):
        project = (ROOT/'BankNotificationWorker/BankNotificationWorker.csproj').read_text()
        self.assertIn('<OutputType>Exe</OutputType>', project)
        self.assertIn('<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>', project)
        sln = (ROOT/'AshkanIndustrialAccounting.sln').read_text(encoding='utf-8-sig')
        self.assertIn('BankNotificationWorker\\BankNotificationWorker.csproj', sln)

    def test_only_critical_new_or_reopened_alerts_enqueue(self):
        service = (ROOT/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text(encoding='utf-8-sig')
        self.assertIn('notifyCritical = severity == "Critical";', service)
        self.assertIn('severity == "Critical" && Convert.ToString(state.ExecuteScalar()) == "Resolved"', service)
        self.assertIn('INSERT dbo.BankAlertNotificationOutbox', service)
        self.assertIn('tx.Commit(); return id;', service)

    def test_worker_waits_for_assignee_instead_of_losing_notification(self):
        worker = (ROOT/'BankNotificationWorker/Program.cs').read_text(encoding='utf-8')
        self.assertIn('a.AssignedToUserId IS NOT NULL', worker)
        self.assertIn('WHERE o.ProcessedAtUtc IS NULL', worker)

    def test_no_external_broker_dependency(self):
        project = (ROOT/'BankNotificationWorker/BankNotificationWorker.csproj').read_text()
        self.assertNotIn('PackageReference', project)
        self.assertNotIn('RabbitMQ', project)

if __name__ == '__main__': unittest.main()
