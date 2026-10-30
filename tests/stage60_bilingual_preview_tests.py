from pathlib import Path
root=Path(__file__).resolve().parents[1]/"AshkanIndustrialAccounting"
validator=(root/"Services/BankImportPreviewValidator.cs").read_text()
form=(root/"UI/Forms/BankReconciliationForm.cs").read_text()
assert "return Validate(preview, existing, false);" in validator
assert "bool persian" in validator
for message in ["تاریخ نامعتبر", "شناسه مرجع الزامی", "مبلغ نامعتبر", "شرح بیشتر", "شناسه مرجع تکراری", "پایگاه داده"]: assert message in validator
assert 'error == null ? "Ready"' in validator
assert 'Validate(table, existing, LocalizationService.IsPersian)' in form
assert 'Validate(table,latest,LocalizationService.IsPersian)' in form
assert 'RightToLeft.Yes' in form and 'AutoSizeColumnsMode.Fill' in form
assert 'A reference was already imported. Review again.' in form and 'قبلاً ثبت شده' in form
print("Stage 60 source contract checks passed (12 assertions)")
