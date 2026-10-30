from pathlib import Path
root=Path(__file__).resolve().parents[1]/"AshkanIndustrialAccounting"
form=(root/"UI/Forms/BankReconciliationForm.cs").read_text()
service=(root/"Services/BankReconciliationService.cs").read_text()
assert "validated=BankImportPreviewValidator.Validate(table,latest" in form
assert "existing=latest;" in form
assert "ex.Number==2601 || ex.Number==2627" in form
assert "catch(System.Data.SqlClient.SqlException ex)" in form
assert "BeginTransaction(IsolationLevel.Serializable)" in service
assert "tx.Commit();" in service
assert "TransactionalAuditService.Record" in service
print("Stage61: 7 source-contract checks passed")
