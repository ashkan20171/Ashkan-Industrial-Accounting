from pathlib import Path
s=(Path(__file__).resolve().parents[1]/"AshkanIndustrialAccounting/Services/BankStatementXlsx.cs").read_text()
checks=["sourceRows[i + offset]", "sourceRows.Add(physicalRow)", 'row.Attributes["r"]', "physicalRow <= lastPhysicalRow", "physicalRow > 1048576", "(!hasHeader || count>0)"]
for c in checks: assert c in s,c
print("6 Stage59 structural checks passed")
