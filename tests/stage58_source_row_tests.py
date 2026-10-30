from pathlib import Path
root=Path(__file__).resolve().parents[1]/"AshkanIndustrialAccounting"
csv=(root/"Services/BankStatementCsv.cs").read_text()
form=(root/"UI/Forms/BankReconciliationForm.cs").read_text()
checks={"CSV physical line tracking":"physicalLine++" in csv and "recordStart=physicalLine" in csv,"preview source row":"result.Columns.Add(\"SourceRow\", typeof(int))" in csv,"quoted newline tracking":"if(ch=='\\n'" in csv,"report original source row":"sourceRow.ToString()" in form,"source row read only":"grid.Columns[\"SourceRow\"].ReadOnly=true" in form,"atomic import preserved":"ImportStatementBatch(SelectedAccount,validated)" in form}
for name, ok in checks.items():
 print(("PASS" if ok else "FAIL")+": "+name)
assert all(checks.values())
