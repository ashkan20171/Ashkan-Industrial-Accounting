from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]

class OperationsCenterTests(unittest.TestCase):
    def test_history_migration_creates_table(self):
        sql=(ROOT/'database/025_OperationsCenterHistory.sql').read_text(encoding='utf-8')
        self.assertIn('CREATE TABLE dbo.BackgroundWorkerHistory', sql)
        self.assertIn('IX_BackgroundWorkerHistory_Heartbeat', sql)
    def test_history_status_constraint(self):
        sql=(ROOT/'database/025_OperationsCenterHistory.sql').read_text(encoding='utf-8')
        self.assertIn("N'Healthy',N'Warning',N'Error'", sql)
    def test_bank_worker_persists_history(self):
        src=(ROOT/'BankNotificationWorker/Program.cs').read_text(encoding='utf-8')
        self.assertIn('INSERT dbo.BackgroundWorkerHistory', src)
    def test_email_worker_persists_history(self):
        src=(ROOT/'EmailNotificationWorker/Program.cs').read_text(encoding='utf-8')
        self.assertIn('INSERT dbo.BackgroundWorkerHistory', src)
    def test_recent_history_is_authorized(self):
        src=(ROOT/'AshkanIndustrialAccounting/Services/BackgroundWorkerMonitoringService.cs').read_text(encoding='utf-8')
        self.assertIn('RequireActiveSession("services")', src)
        self.assertIn('GetRecentHistory(int hours)', src)
    def test_history_query_is_parameterized_and_bounded(self):
        src=(ROOT/'AshkanIndustrialAccounting/Services/BackgroundWorkerMonitoringService.cs').read_text(encoding='utf-8')
        self.assertIn('cmd.Parameters.Add("@hours"', src)
        self.assertIn('hours > 168', src)
    def test_daily_metrics_query_available(self):
        src=(ROOT/'AshkanIndustrialAccounting/Services/BackgroundWorkerMonitoringService.cs').read_text(encoding='utf-8')
        self.assertIn('GetDailyMetrics(int days)', src)
        self.assertIn('HealthyCount', src)
        self.assertIn('ErrorCount', src)
    def test_ui_shows_recent_history(self):
        src=(ROOT/'AshkanIndustrialAccounting/UI/Forms/ServiceMonitoringForm.cs').read_text(encoding='utf-8')
        self.assertIn('GetRecentHistory(24)', src)
        self.assertIn('_history.DataSource', src)

if __name__ == '__main__': unittest.main()
