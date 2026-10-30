"""Static regression contracts. Does not test actual SQL authorization or rollback."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]/'AshkanIndustrialAccounting'
class SecurityContracts(unittest.TestCase):
    def source(self,path):return (ROOT/path).read_text(encoding='utf-8-sig')
    def test_journal_write_checks_session(self):
        src=self.source('Infrastructure/Repositories/JournalRepository.cs')
        self.assertIn('AuthorizationService.RequireActiveSession("journal")',src)
    def test_journal_audit_is_before_commit(self):
        src=self.source('Infrastructure/Repositories/JournalRepository.cs')
        self.assertLess(src.index('TransactionalAuditService.Record'),src.index('tx.Commit()'))
    def test_chart_of_accounts_mutations_check_session(self):
        src=self.source('Infrastructure/Repositories/AccountRepository.cs')
        self.assertEqual(src.count('AuthorizationService.RequireActiveSession("accounts")'),3)
    def test_accounting_service_checks_session(self):
        self.assertIn('AuthorizationService.RequireActiveSession("journal")',self.source('Services/AccountingService.cs'))
if __name__=='__main__':unittest.main()
