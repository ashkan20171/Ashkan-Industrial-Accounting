from pathlib import Path
root = Path(__file__).resolve().parents[1]
service = (root / "AshkanIndustrialAccounting/Services/BankReconciliationService.cs").read_text(encoding="utf-8-sig")
form = (root / "AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs").read_text(encoding="utf-8-sig")
checks = {
    "all-filtered export service exists": "GetImportHistoryForExport" in service,
    "export query has bounded TOP limit": "SELECT TOP (@limit)" in service and "maximumRows + 1" in service,
    "export uses account/date/user filters": all(x in service for x in ["CashBankAccountId=@account", "ImportedAt>=@from", "ImportedAt<@until", "ImportedBy LIKE N'%' + @user + N'%'"]),
    "export has deterministic ordering": "ORDER BY ImportedAt DESC, ImportId DESC" in service,
    "export detects over-limit results": "table.Rows.Count > maximumRows" in service,
    "CSV export uses full filtered data": "DataTable exportData=getFilteredExportData()" in form and "foreach(DataRow row in exportData.Rows)" in form,
    "CSV uses UTF-8 BOM": "new UTF8Encoding(true)" in form,
    "formula protection handles leading whitespace": "Char.IsWhiteSpace(value[index]) || Char.IsControl(value[index])" in form,
    "print preview uses all filtered rows": "DataTable printData=getFilteredExportData()" in form and "printArgs.HasMorePages=rowIndex<printData.Rows.Count" in form,
    "print document uses landscape": "document.DefaultPageSettings.Landscape=true" in form,
}
for name, ok in checks.items():
    print(("PASS" if ok else "FAIL") + " - " + name)
assert all(checks.values())
