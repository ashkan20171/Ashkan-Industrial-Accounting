from pathlib import Path
s=Path(__file__).resolve().parents[1].joinpath("AshkanIndustrialAccounting/UI/Forms/BankReconciliationForm.cs").read_text()
checks=["Days with failures", "FailedImports > 0", "TotalDays>366", "days.Sort", "days[n-1-i]", "value.TrimStart()", "grid.DataSource=view", "STAGE69"]
for check in checks[:-1]:
    assert check in s, check
print("7 Stage 69 source-contract checks passed (not C# runtime tests)")
