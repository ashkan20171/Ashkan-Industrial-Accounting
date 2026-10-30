from pathlib import Path
root=Path(__file__).resolve().parents[1]/"AshkanIndustrialAccounting"
csv=(root/"Services/BankStatementCsv.cs").read_text()
xlsx=(root/"Services/BankStatementXlsx.cs").read_text()
form=(root/"UI/Forms/BankReconciliationForm.cs").read_text()
assert "public static DataTable ReadPreview" in csv
assert 'values[4] = "Needs validation"' in csv
assert "public static DataTable ReadPreview" in xlsx
assert "ReadCsvContent(path, columns)" in xlsx
assert "BankStatementCsv.ReadPreview" in form
assert "Color.MistyRose" in form and "Color.Honeydew" in form
assert "ImportStatementBatch" in form
print("7 stage56 structural assertions passed")
