import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class WorkerMonitoringTests(unittest.TestCase):
 def read(self,p): return (ROOT/p).read_text(encoding='utf-8-sig')
 def test_migration_creates_heartbeat_table(self):
  s=self.read('database/024_BackgroundWorkerMonitoring.sql'); self.assertIn('BackgroundWorkerStatus',s); self.assertIn('LastHeartbeatUtc',s); self.assertIn('LastError',s)
 def test_both_workers_write_heartbeat(self):
  for p in ['EmailNotificationWorker/Program.cs','BankNotificationWorker/Program.cs']:
   s=self.read(p); self.assertIn('WriteHeartbeat(cycleError)',s); self.assertIn('BackgroundWorkerStatus',s)
 def test_worker_names_are_distinct(self):
  self.assertIn('EmailNotificationWorker',self.read('EmailNotificationWorker/Program.cs'))
  self.assertIn('BankNotificationWorker',self.read('BankNotificationWorker/Program.cs'))
 def test_monitor_requires_services_permission(self):
  s=self.read('AshkanIndustrialAccounting/Services/BackgroundWorkerMonitoringService.cs'); self.assertGreaterEqual(s.count('RequireActiveSession("services")'),2)
 def test_services_permission_is_admin_only(self):
  self.assertIn('case "services": return CurrentRole == Administrator',self.read('AshkanIndustrialAccounting/Services/AuthorizationService.cs'))
 def test_queue_summary_includes_both_queues(self):
  s=self.read('AshkanIndustrialAccounting/Services/BackgroundWorkerMonitoringService.cs'); self.assertIn('BankAlertNotificationOutbox',s); self.assertIn('EmailNotificationOutbox',s)
 def test_screen_is_bilingual_and_refreshes(self):
  s=self.read('AshkanIndustrialAccounting/UI/Forms/ServiceMonitoringForm.cs'); self.assertIn('LocalizationService.IsPersian',s); self.assertIn('_timer.Interval=15000',s)
 def test_project_includes_new_sources(self):
  s=self.read('AshkanIndustrialAccounting/AshkanIndustrialAccounting.csproj'); self.assertIn('BackgroundWorkerMonitoringService.cs',s); self.assertIn('ServiceMonitoringForm.cs',s)
if __name__=='__main__': unittest.main()
