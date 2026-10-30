from pathlib import Path
root = Path(__file__).resolve().parents[1]
service = (root / "AshkanIndustrialAccounting/Services/BankReconciliationService.cs").read_text(encoding="utf-8-sig")
form = (root / "AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs").read_text(encoding="utf-8-sig")
checks = {
    "count method exists": "GetImportHistoryCount" in service,
    "count uses server aggregation": "COUNT_BIG(*)" in service,
    "count filters account and dates": "CashBankAccountId=@account AND ImportedAt>=@from AND ImportedAt<@until" in service,
    "count filters user": "ImportedBy LIKE N'%' + @user + N'%'" in service,
    "count uses parameterized user": 'cmd.Parameters.Add("@user", SqlDbType.NVarChar, 100)' in service,
    "page UI shows total rows": "Total rows:" in form or "کل رکوردها:" in form,
    "next uses total page count": "pageIndex+1<totalPages" in form,
    "page query remains parameterized": 'cmd.Parameters.Add("@offset", SqlDbType.Int)' in service,
}
for name, ok in checks.items():
    print(("PASS" if ok else "FAIL") + " - " + name)
assert all(checks.values())
