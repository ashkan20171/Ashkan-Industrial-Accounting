from pathlib import Path
root=Path(__file__).resolve().parents[1]/"AshkanIndustrialAccounting"
service=(root/"Services/BankReconciliationService.cs").read_text()
form=(root/"UI/Forms/BankReconciliationForm.cs").read_text()
checks=["GetImportDetails(int accountId, Guid importId)" in service,
        "h.CashBankAccountId=@account" in service,
        "l.CashBankAccountId=@account" in service,
        "h.ImportId=@import" in service,
        "view.RowFilter" in form,
        "foreach (DataRowView row in view)" in form,
        "GetImportDetails(SelectedAccount,importId)" in form]
assert all(checks),checks
print("7 Stage63 source-contract checks passed")
