using System;
using System.Data;
using System.Data.SqlClient;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>
    /// Balanced inventory entry using a clearing account. A receipt is NOT
    /// automatically a supplier invoice; settlement must be reconciled separately.
    /// Runs inside the physical inventory movement transaction.
    /// </summary>
    internal static class InventoryJournalService
    {
        internal static void Record(SqlConnection connection, SqlTransaction transaction,
            Guid operationId, string movementType, decimal valuationDelta)
        {
            if (valuationDelta == 0m) return;
            int inventoryAccount = Account(connection, transaction, "1300");
            int clearingAccount = Account(connection, transaction, "1390");
            string entryNo = "IM-" + operationId.ToString("N"); // 35 chars; unique on retry
            int journalId;
            using (var command = new SqlCommand(@"INSERT dbo.JournalEntries
(EntryNo,EntryDate,Description,ReferenceNo,IsPosted)
VALUES(@no,SYSUTCDATETIME(),@description,@reference,1);
SELECT CAST(SCOPE_IDENTITY() AS INT)", connection, transaction))
            {
                command.Parameters.Add("@no", SqlDbType.NVarChar, 40).Value = entryNo;
                command.Parameters.Add("@description", SqlDbType.NVarChar, 500).Value =
                    "Inventory movement: " + movementType;
                command.Parameters.Add("@reference", SqlDbType.NVarChar, 80).Value =
                    operationId.ToString("D");
                journalId = Convert.ToInt32(command.ExecuteScalar());
            }
            decimal amount = Math.Abs(valuationDelta);
            if (valuationDelta > 0m)
            {
                Line(connection, transaction, journalId, inventoryAccount, amount, 0m);
                Line(connection, transaction, journalId, clearingAccount, 0m, amount);
            }
            else
            {
                Line(connection, transaction, journalId, inventoryAccount, 0m, amount);
                Line(connection, transaction, journalId, clearingAccount, amount, 0m);
            }
        }

        private static int Account(SqlConnection cn, SqlTransaction tx, string code)
        {
            using (var command = new SqlCommand(
                "SELECT Id FROM dbo.Accounts WHERE Code=@code AND IsActive=1", cn, tx))
            {
                command.Parameters.Add("@code", SqlDbType.NVarChar, 30).Value = code;
                var value = command.ExecuteScalar();
                if (value == null) throw new InvalidOperationException(
                    "Required active inventory journal account is missing: " + code);
                return Convert.ToInt32(value);
            }
        }

        private static void Line(SqlConnection cn, SqlTransaction tx, int journalId,
            int accountId, decimal debit, decimal credit)
        {
            using (var command = new SqlCommand(@"INSERT dbo.JournalLines
(JournalEntryId,AccountId,Description,Debit,Credit)
VALUES(@journal,@account,N'Inventory valuation movement',@debit,@credit)", cn, tx))
            {
                command.Parameters.Add("@journal", SqlDbType.Int).Value = journalId;
                command.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                Money(command, "@debit", debit);
                Money(command, "@credit", credit);
                if (command.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Failed to insert journal line");
            }
        }

        private static void Money(SqlCommand command, string key, decimal value)
        {
            var parameter = command.Parameters.Add(key, SqlDbType.Decimal);
            parameter.Precision = 18;
            parameter.Scale = 2;
            parameter.Value = value;
        }
    }
}
