from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
FORM=(ROOT/"AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs").read_text(encoding="utf-8-sig")
class Stage77Tests(unittest.TestCase):
 def test_failure_rate_formula(self): self.assertIn("bad/(ok+bad))*100m",FORM)
 def test_zero_attempt_guard(self): self.assertIn("(ok+bad)==0?0m",FORM)
 def test_review_threshold(self): self.assertIn("rate>=10m",FORM)
 def test_rank_sort(self): self.assertIn("FailureRate DESC, FailedImports DESC, Name ASC",FORM)
 def test_three_filters(self): self.assertIn("FailedImports > 0",FORM); self.assertIn("ReviewStatus = 'Review'",FORM)
 def test_summary_metrics(self): self.assertIn("Highest failure rate:",FORM); self.assertIn("بیشترین نرخ خطا",FORM)
 def test_export_respects_view(self): self.assertIn("var exportData=visible.ToTable()",FORM)
 def test_existing_query_reused(self): self.assertIn("GetAccountImportComparison(from.Value.Date",FORM)
if __name__=="__main__": unittest.main()
