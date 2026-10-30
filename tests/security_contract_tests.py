"""Run: python tests/security_contract_tests.py (static security regression checks)."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]/"AshkanIndustrialAccounting"
services=root/"Services"
checks={
 "SalesPostingService.cs": ["RequireActiveSession(\"sales\")", "AuthorizationService.CurrentUserName"],
 "SalesReturnService.cs": ["RequireActiveSession(\"sales\")", "AuthorizationService.CurrentUserName"],
 "InventoryService.cs": ["RequireActiveSession(\"products\")", "AuthorizationService.CurrentUserName"],
 "InvoiceDraftService.cs": ["RequireActiveSession(\"sales\")"],
 "UserManagementService.cs": ["RequireActiveSession(\"users\")"],
}
for file, needles in checks.items():
    content=(services/file).read_text(encoding="utf-8-sig")
    for needle in needles:
        assert needle in content, f"{file}: missing {needle}"
form=(root/"UI/Forms/SalesForm.cs").read_text(encoding="utf-8-sig")
assert "SalesReturnService.ReturnPosted(id, AuthorizationService.CurrentUserName)" in form
assert "SalesReturnService.ReturnPosted(id, Environment.UserName)" not in form
print("PASS: security source-contract checks")
