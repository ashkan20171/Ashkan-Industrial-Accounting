from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
SERVICE=(ROOT/'AshkanIndustrialAccounting/Services/BankReconciliationService.cs').read_text(encoding='utf-8-sig')
FORM=(ROOT/'AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs').read_text(encoding='utf-8-sig')
SQL=(ROOT/'database/020_BankImportAlertWorkflow.sql').read_text(encoding='utf-8-sig')
class Stage80AlertWorkflow(unittest.TestCase):
 def test_migration_adds_assignment_and_due_date(self):
  self.assertIn('AssignedToUserId',SQL); self.assertIn('DueAtUtc',SQL)
 def test_priority_is_constrained_in_service(self):
  for p in ('Low','Normal','High','Urgent'): self.assertIn('"'+p+'"',SERVICE)
 def test_notes_and_history_tables_exist(self):
  self.assertIn('CREATE TABLE dbo.BankImportAlertNotes',SQL); self.assertIn('CREATE TABLE dbo.BankImportAlertHistory',SQL)
 def test_history_is_transactional(self):
  self.assertIn('RecordAlertHistory(cn,tx,alertId,"Status"',SERVICE)
  self.assertIn('RecordAlertHistory(cn,tx,alertId,"Assignment"',SERVICE)
 def test_assignee_must_be_active(self): self.assertIn('IsActive=1 AND (LockedUntil IS NULL',SERVICE)
 def test_note_length_is_validated(self): self.assertIn('note.Trim().Length>1000',SERVICE)
 def test_due_and_priority_are_persisted(self): self.assertIn('SetBankAlertSchedule',SERVICE); self.assertIn('DueAtUtc=@due,PriorityCode=@priority',SERVICE)
 def test_overdue_is_projected(self): self.assertIn('AS IsOverdue',SERVICE)
 def test_ui_exposes_workflow_actions(self):
  for method in ('AssignBankAlert','SetBankAlertSchedule','AddBankAlertNote','GetBankAlertHistory','GetBankAlertNotes'):
   self.assertIn(method,FORM)
if __name__=='__main__': unittest.main()
