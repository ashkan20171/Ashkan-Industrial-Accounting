using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Manual statement entry and strictly validated one-to-one reconciliation.</summary>
    public static class BankReconciliationService
    {
        /// <summary>Return existing bank references for a single account using a parameterized query.</summary>
        public static HashSet<string> FindExistingReferences(int accountId, IEnumerable<string> references)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0) throw new ArgumentException("Invalid account.");
            if (references == null) throw new ArgumentNullException("references");
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string reference in references)
            {
                if (String.IsNullOrWhiteSpace(reference) || reference.Trim().Length > 100)
                    throw new ArgumentException("Invalid bank reference.");
                unique.Add(reference.Trim());
            }
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (unique.Count == 0) return existing;
            using (var cn = Db.Open())
            {
                // Chunk to remain below SQL Server's 2100-parameter limit.
                var batch = new List<string>(500);
                foreach (string reference in unique)
                {
                    batch.Add(reference);
                    if (batch.Count == 500) { FindBatch(cn, accountId, batch, existing); batch.Clear(); }
                }
                if (batch.Count > 0) FindBatch(cn, accountId, batch, existing);
            }
            return existing;
        }
        private static void FindBatch(SqlConnection cn, int accountId, List<string> batch, HashSet<string> existing)
        {
            using (var cmd = cn.CreateCommand())
            {
                var names = new List<string>();
                cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                for (int i = 0; i < batch.Count; i++)
                {
                    string name = "@r" + i;
                    names.Add(name);
                    cmd.Parameters.Add(name, SqlDbType.NVarChar, 100).Value = batch[i];
                }
                cmd.CommandText = "SELECT ExternalReference FROM dbo.BankStatementLines WHERE CashBankAccountId=@account AND ExternalReference IN (" + String.Join(",", names.ToArray()) + ")";
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) existing.Add(reader.GetString(0));
            }
        }

        /// <summary>All-or-nothing import. Duplicate references and invalid rows abort the entire batch.</summary>
        public static int ImportStatementBatch(int accountId, IList<BankStatementCsvRow> rows)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0 || rows == null || rows.Count == 0 || rows.Count > 10000)
                throw new ArgumentException("Invalid account or import batch.");
            var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (row == null || String.IsNullOrWhiteSpace(row.Reference) || row.Reference.Trim().Length > 100 ||
                    (row.Description ?? "").Length > 500 || row.Amount == 0 ||
                    decimal.Round(row.Amount, 2) != row.Amount || Math.Abs(row.Amount) > 9999999999999999m)
                    throw new ArgumentException("Invalid bank statement row.");
                if (!references.Add(row.Reference.Trim()))
                    throw new InvalidOperationException("Duplicate bank reference in import: " + row.Reference);
            }
            using (var cn = Db.Open())
            using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
            {
                using (var account = new SqlCommand(@"SELECT COUNT(*) FROM dbo.CashBankAccounts WITH (UPDLOCK,HOLDLOCK)
WHERE Id=@id AND IsActive=1 AND CurrencyCode=N'IRR'", cn, tx))
                {
                    account.Parameters.Add("@id", SqlDbType.Int).Value = accountId;
                    if (Convert.ToInt32(account.ExecuteScalar()) != 1)
                        throw new InvalidOperationException("Active IRR bank account required.");
                }
                // A single import identifier connects all statement lines to their history.
                Guid importId = Guid.NewGuid();
                using (var history = new SqlCommand(@"INSERT dbo.BankImportHistory
(ImportId,CashBankAccountId,ImportedBy,RowCount,TotalIncoming,TotalOutgoing)
VALUES(@id,@account,@user,@count,@incoming,@outgoing)", cn, tx))
                {
                    history.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = importId;
                    history.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                    history.Parameters.Add("@user", SqlDbType.NVarChar, 100).Value = AuthorizationService.CurrentUserName;
                    history.Parameters.Add("@count", SqlDbType.Int).Value = rows.Count;
                    decimal incoming = 0m, outgoing = 0m;
                    foreach (var item in rows) { if (item.Amount > 0) incoming += item.Amount; else outgoing -= item.Amount; }
                    var pIn = history.Parameters.Add("@incoming", SqlDbType.Decimal); pIn.Precision=22; pIn.Scale=2; pIn.Value=incoming;
                    var pOut = history.Parameters.Add("@outgoing", SqlDbType.Decimal); pOut.Precision=22; pOut.Scale=2; pOut.Value=outgoing;
                    history.ExecuteNonQuery();
                }
                int count = 0;
                foreach (var row in rows)
                {
                    // The unique (account, reference) constraint remains the final concurrency guard.
                    int id;
                    using (var cmd = new SqlCommand(@"INSERT dbo.BankStatementLines
(CashBankAccountId,StatementDate,ExternalReference,Description,Amount,ImportedBy,ImportId)
OUTPUT INSERTED.Id VALUES(@a,@d,@r,@note,@amount,@user,@import)", cn, tx))
                    {
                        cmd.Parameters.Add("@a", SqlDbType.Int).Value = accountId;
                        cmd.Parameters.Add("@d", SqlDbType.Date).Value = row.Date.Date;
                        cmd.Parameters.Add("@r", SqlDbType.NVarChar,100).Value = row.Reference.Trim();
                        cmd.Parameters.Add("@note", SqlDbType.NVarChar,500).Value = (object)row.Description ?? DBNull.Value;
                        var amount = cmd.Parameters.Add("@amount", SqlDbType.Decimal);
                        amount.Precision = 18; amount.Scale = 2; amount.Value = row.Amount;
                        cmd.Parameters.Add("@user", SqlDbType.NVarChar,100).Value = AuthorizationService.CurrentUserName;
                        cmd.Parameters.Add("@import", SqlDbType.UniqueIdentifier).Value = importId;
                        id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    TransactionalAuditService.Record(cn, tx, AuthorizationService.CurrentUserName,
                        "BankStatementImported", "BankStatementLine", id.ToString(), row.Reference.Trim());
                    count++;
                }
                tx.Commit();
                return count;
            }
        }

        /// <summary>Best-effort failure audit on a NEW connection, independent of rolled-back imports.
        /// Never persist exception messages, filenames, statement values, or other sensitive payloads.</summary>
        public static void TryRecordImportFailure(int accountId, int attemptedRows, string failureCode)
        {
            try
            {
                AuthorizationService.RequireActiveSession("cashbank");
                if (accountId <= 0) return;
                string code = failureCode == "DuplicateReference" ? "DuplicateReference" :
                    failureCode == "DatabaseError" ? "DatabaseError" : "ValidationError";
                using (var cn = Db.Open())
                using (var cmd = new SqlCommand(@"INSERT dbo.BankImportFailureAudit
(CashBankAccountId, AttemptedRows, FailureCode, AttemptedBy)
VALUES (@account, @count, @code, @user)", cn))
                {
                    cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                    cmd.Parameters.Add("@count", SqlDbType.Int).Value = Math.Max(0, Math.Min(10000, attemptedRows));
                    cmd.Parameters.Add("@code", SqlDbType.NVarChar, 40).Value = code;
                    cmd.Parameters.Add("@user", SqlDbType.NVarChar, 100).Value = AuthorizationService.CurrentUserName;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception) { /* Audit failure must never mask the original import error. */ }
        }

        /// <summary>Recent failed imports for the selected bank account.</summary>
        public static DataTable GetImportFailures(int accountId)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0) throw new ArgumentException("Invalid account.");
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT TOP (200) AttemptedAt, AttemptedBy,
AttemptedRows, FailureCode FROM dbo.BankImportFailureAudit
WHERE CashBankAccountId=@account ORDER BY AttemptedAt DESC, Id DESC", cn))
            {
                cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                using (var adapter = new SqlDataAdapter(cmd))
                { var result = new DataTable(); adapter.Fill(result); return result; }
            }
        }

        /// <summary>Server-side, account-scoped UTC aggregate; no TOP (200) truncation.</summary>
        public static DataTable GetImportAnalytics(int accountId, DateTime fromUtc, DateTime untilUtcExclusive)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0 || fromUtc >= untilUtcExclusive)
                throw new ArgumentException("Invalid account or date range.");
            const string sql = @"SELECT x.DayUtc, SUM(x.SuccessCount) AS SuccessfulImports,
 SUM(x.FailureCount) AS FailedImports, SUM(x.ImportedRows) AS ImportedRows,
 SUM(x.Incoming) AS TotalIncoming, SUM(x.Outgoing) AS TotalOutgoing
 FROM (
 SELECT CONVERT(date, ImportedAt) AS DayUtc, COUNT_BIG(*) AS SuccessCount,
 CAST(0 AS bigint) AS FailureCount, SUM(CAST(RowCount AS bigint)) AS ImportedRows,
 SUM(TotalIncoming) AS Incoming, SUM(TotalOutgoing) AS Outgoing
 FROM dbo.BankImportHistory
 WHERE CashBankAccountId=@account AND ImportedAt>=@from AND ImportedAt<@until
 GROUP BY CONVERT(date, ImportedAt)
 UNION ALL
 SELECT CONVERT(date, AttemptedAt), CAST(0 AS bigint), COUNT_BIG(*),
 CAST(0 AS bigint), CAST(0 AS decimal(38,2)), CAST(0 AS decimal(38,2))
 FROM dbo.BankImportFailureAudit
 WHERE CashBankAccountId=@account AND AttemptedAt>=@from AND AttemptedAt<@until
 GROUP BY CONVERT(date, AttemptedAt)
 ) x GROUP BY x.DayUtc ORDER BY x.DayUtc DESC";
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                cmd.Parameters.Add("@from", SqlDbType.DateTime2).Value = fromUtc;
                cmd.Parameters.Add("@until", SqlDbType.DateTime2).Value = untilUtcExclusive;
                using (var adapter = new SqlDataAdapter(cmd))
                { var result = new DataTable(); adapter.Fill(result); return result; }
            }
        }

        /// <summary>Compare all active IRR accounts without multiplying success/failure rows.</summary>
        public static DataTable GetAccountImportComparison(DateTime fromUtc, DateTime untilUtcExclusive)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (fromUtc >= untilUtcExclusive || (untilUtcExclusive-fromUtc).TotalDays > 367)
                throw new ArgumentException("Invalid comparison date range.");
            const string sql = @"SELECT a.Id AS AccountId, a.Code, a.Name,
 ISNULL(s.SuccessfulImports,0) AS SuccessfulImports,
 ISNULL(f.FailedImports,0) AS FailedImports,
 ISNULL(s.ImportedRows,0) AS ImportedRows,
 ISNULL(s.TotalIncoming,0) AS TotalIncoming,
 ISNULL(s.TotalOutgoing,0) AS TotalOutgoing
 FROM dbo.CashBankAccounts a
 LEFT JOIN (SELECT CashBankAccountId, COUNT_BIG(*) AS SuccessfulImports,
 SUM(CAST(RowCount AS bigint)) AS ImportedRows,
 SUM(TotalIncoming) AS TotalIncoming, SUM(TotalOutgoing) AS TotalOutgoing
 FROM dbo.BankImportHistory
 WHERE ImportedAt >= @from AND ImportedAt < @until
 GROUP BY CashBankAccountId) s ON s.CashBankAccountId=a.Id
 LEFT JOIN (SELECT CashBankAccountId, COUNT_BIG(*) AS FailedImports
 FROM dbo.BankImportFailureAudit
 WHERE AttemptedAt >= @from AND AttemptedAt < @until
 GROUP BY CashBankAccountId) f ON f.CashBankAccountId=a.Id
 WHERE a.IsActive=1 AND a.CurrencyCode=N'IRR'
 ORDER BY a.Name, a.Id";
            using (var cn=Db.Open())
            using (var cmd=new SqlCommand(sql,cn))
            {
                cmd.Parameters.Add("@from",SqlDbType.DateTime2).Value=fromUtc;
                cmd.Parameters.Add("@until",SqlDbType.DateTime2).Value=untilUtcExclusive;
                using (var da=new SqlDataAdapter(cmd))
                { var result=new DataTable(); da.Fill(result); return result; }
            }
        }

        /// <summary>Server-side paging and filters, with a deterministic tie-breaker.</summary>
        public static DataTable GetImportHistoryPage(int accountId, DateTime fromUtc,
            DateTime untilUtcExclusive, string importedBy, int pageIndex, int pageSize)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0 || fromUtc >= untilUtcExclusive ||
                (untilUtcExclusive-fromUtc).TotalDays > 367 || pageIndex < 0 ||
                pageSize < 1 || pageSize > 200 || pageIndex > 100000)
                throw new ArgumentException("Invalid import history query.");
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT ImportId, ImportedAt, ImportedBy,
RowCount, TotalIncoming, TotalOutgoing
FROM dbo.BankImportHistory
WHERE CashBankAccountId=@account AND ImportedAt>=@from AND ImportedAt<@until
AND (@user=N'' OR ImportedBy LIKE N'%' + @user + N'%')
ORDER BY ImportedAt DESC, ImportId DESC
OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY", cn))
            {
                cmd.CommandTimeout = 45;
                cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                cmd.Parameters.Add("@from", SqlDbType.DateTime2).Value = fromUtc;
                cmd.Parameters.Add("@until", SqlDbType.DateTime2).Value = untilUtcExclusive;
                cmd.Parameters.Add("@user", SqlDbType.NVarChar, 100).Value =
                    (importedBy ?? "").Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                cmd.Parameters.Add("@offset", SqlDbType.Int).Value = checked(pageIndex * pageSize);
                cmd.Parameters.Add("@size", SqlDbType.Int).Value = pageSize;
                using (var adapter = new SqlDataAdapter(cmd))
                { var table = new DataTable(); adapter.Fill(table); return table; }
            }
        }

        /// <summary>Returns the exact number of history rows matching the same server-side filters as paging.</summary>
        public static int GetImportHistoryCount(int accountId, DateTime fromUtc,
            DateTime untilUtcExclusive, string importedBy)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0 || fromUtc >= untilUtcExclusive ||
                (untilUtcExclusive-fromUtc).TotalDays > 367)
                throw new ArgumentException("Invalid import history count query.");
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT COUNT_BIG(*)
FROM dbo.BankImportHistory
WHERE CashBankAccountId=@account AND ImportedAt>=@from AND ImportedAt<@until
AND (@user=N'' OR ImportedBy LIKE N'%' + @user + N'%')", cn))
            {
                cmd.CommandTimeout = 45;
                cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                cmd.Parameters.Add("@from", SqlDbType.DateTime2).Value = fromUtc;
                cmd.Parameters.Add("@until", SqlDbType.DateTime2).Value = untilUtcExclusive;
                cmd.Parameters.Add("@user", SqlDbType.NVarChar, 100).Value =
                    (importedBy ?? "").Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                long count = Convert.ToInt64(cmd.ExecuteScalar());
                return count > Int32.MaxValue ? Int32.MaxValue : (int)count;
            }
        }

        /// <summary>Returns every row matching the history filters, with a hard export limit.</summary>
        public static DataTable GetImportHistoryForExport(int accountId, DateTime fromUtc,
            DateTime untilUtcExclusive, string importedBy)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0 || fromUtc >= untilUtcExclusive ||
                (untilUtcExclusive-fromUtc).TotalDays > 367)
                throw new ArgumentException("Invalid import history export query.");
            const int maximumRows = 50000;
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT TOP (@limit) ImportId, ImportedAt, ImportedBy,
RowCount, TotalIncoming, TotalOutgoing
FROM dbo.BankImportHistory
WHERE CashBankAccountId=@account AND ImportedAt>=@from AND ImportedAt<@until
AND (@user=N'' OR ImportedBy LIKE N'%' + @user + N'%')
ORDER BY ImportedAt DESC, ImportId DESC", cn))
            {
                cmd.CommandTimeout = 60;
                cmd.Parameters.Add("@limit", SqlDbType.Int).Value = maximumRows + 1;
                cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                cmd.Parameters.Add("@from", SqlDbType.DateTime2).Value = fromUtc;
                cmd.Parameters.Add("@until", SqlDbType.DateTime2).Value = untilUtcExclusive;
                cmd.Parameters.Add("@user", SqlDbType.NVarChar, 100).Value =
                    (importedBy ?? "").Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var table = new DataTable(); adapter.Fill(table);
                    if (table.Rows.Count > maximumRows)
                        throw new InvalidOperationException("The filtered result exceeds 50,000 rows. Narrow the date range or user filter.");
                    return table;
                }
            }
        }

        /// <summary>Committed batch history for the selected account; newest first.</summary>
        public static DataTable GetImportHistory(int accountId)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0) throw new ArgumentException("Invalid account.");
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT TOP (200) ImportId, ImportedAt, ImportedBy,
RowCount, TotalIncoming, TotalOutgoing
FROM dbo.BankImportHistory WHERE CashBankAccountId=@account
ORDER BY ImportedAt DESC, ImportId DESC", cn))
            {
                cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                using (var da = new SqlDataAdapter(cmd)) { var table = new DataTable(); da.Fill(table); return table; }
            }
        }

        /// <summary>Statement lines for a committed import, scoped to the current account.</summary>
        public static DataTable GetImportDetails(int accountId, Guid importId)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0 || importId == Guid.Empty) throw new ArgumentException("Invalid import selection.");
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT l.Id, l.StatementDate, l.ExternalReference,
 l.Description, l.Amount, l.ImportedBy
FROM dbo.BankStatementLines l
INNER JOIN dbo.BankImportHistory h ON h.ImportId=l.ImportId
WHERE h.CashBankAccountId=@account AND l.CashBankAccountId=@account AND h.ImportId=@import
ORDER BY l.Id", cn))
            {
                cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                cmd.Parameters.Add("@import", SqlDbType.UniqueIdentifier).Value = importId;
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var result = new DataTable(); adapter.Fill(result); return result;
                }
            }
        }

        public static int AddStatementLine(int accountId, DateTime date, string reference, decimal amount, string description)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0 || amount == 0 || decimal.Round(amount, 2) != amount)
                throw new ArgumentException("Invalid account or signed amount.");
            if (String.IsNullOrWhiteSpace(reference) || reference.Trim().Length > 100 || (description ?? "").Length > 500)
                throw new ArgumentException("Invalid reference or description.");
            using (var cn = Db.Open()) using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
            {
                using (var account = new SqlCommand("SELECT COUNT(*) FROM dbo.CashBankAccounts WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id AND IsActive=1 AND CurrencyCode=N'IRR'", cn, tx))
                {
                    account.Parameters.Add("@id", SqlDbType.Int).Value = accountId;
                    if (Convert.ToInt32(account.ExecuteScalar()) != 1) throw new InvalidOperationException("Active IRR account required.");
                }
                int id;
                using (var cmd = new SqlCommand(@"INSERT dbo.BankStatementLines
(CashBankAccountId,StatementDate,ExternalReference,Description,Amount,ImportedBy)
OUTPUT INSERTED.Id VALUES(@a,@d,@r,@note,@amount,@user)", cn, tx))
                {
                    cmd.Parameters.Add("@a", SqlDbType.Int).Value = accountId;
                    cmd.Parameters.Add("@d", SqlDbType.Date).Value = date.Date;
                    cmd.Parameters.Add("@r", SqlDbType.NVarChar,100).Value = reference.Trim();
                    cmd.Parameters.Add("@note", SqlDbType.NVarChar,500).Value = (object)description ?? DBNull.Value;
                    var p = cmd.Parameters.Add("@amount", SqlDbType.Decimal); p.Precision = 18; p.Scale = 2; p.Value = amount;
                    cmd.Parameters.Add("@user", SqlDbType.NVarChar,100).Value = AuthorizationService.CurrentUserName;
                    id = Convert.ToInt32(cmd.ExecuteScalar());
                }
                TransactionalAuditService.Record(cn, tx, AuthorizationService.CurrentUserName,
                    "BankStatementImported", "BankStatementLine", id.ToString(), reference.Trim());
                tx.Commit(); return id;
            }
        }

        public static void MatchPayment(int statementLineId, int paymentId)
        {
            Match(statementLineId, paymentId, false);
        }
        public static void MatchTransfer(int statementLineId, int transferId)
        {
            Match(statementLineId, transferId, true);
        }
        private static void Match(int lineId, int transactionId, bool transfer)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (lineId <= 0 || transactionId <= 0) throw new ArgumentException("Invalid identifiers.");
            using (var cn = Db.Open()) using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
            {
                int accountId; decimal amount;
                using (var line = new SqlCommand(@"SELECT CashBankAccountId,Amount FROM dbo.BankStatementLines WITH (UPDLOCK,HOLDLOCK)
WHERE Id=@id AND MatchedPaymentId IS NULL AND MatchedTransferId IS NULL", cn, tx))
                {
                    line.Parameters.Add("@id", SqlDbType.Int).Value = lineId;
                    using (var reader = line.ExecuteReader())
                    {
                        if (!reader.Read()) throw new InvalidOperationException("Statement line missing or already matched.");
                        accountId = reader.GetInt32(0); amount = reader.GetDecimal(1);
                    }
                }
                var sql = transfer
                    ? @"SELECT CASE WHEN SourceAccountId=@account THEN -Amount WHEN DestinationAccountId=@account THEN Amount ELSE NULL END FROM dbo.CashBankTransfers WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id"
                    : @"SELECT CASE WHEN Direction=N'Receipt' THEN Amount WHEN Direction=N'Payment' THEN -Amount ELSE NULL END FROM dbo.Payments WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id AND CashBankAccountId=@account";
                using (var cmd = new SqlCommand(sql, cn, tx))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = transactionId;
                    cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value || Convert.ToDecimal(result) != amount)
                        throw new InvalidOperationException("Account, direction or amount does not match.");
                }
                using (var cmd = new SqlCommand(transfer
                    ? "UPDATE dbo.BankStatementLines SET MatchedTransferId=@transaction WHERE Id=@line AND MatchedPaymentId IS NULL AND MatchedTransferId IS NULL"
                    : "UPDATE dbo.BankStatementLines SET MatchedPaymentId=@transaction WHERE Id=@line AND MatchedPaymentId IS NULL AND MatchedTransferId IS NULL", cn, tx))
                {
                    cmd.Parameters.Add("@line", SqlDbType.Int).Value = lineId;
                    cmd.Parameters.Add("@transaction", SqlDbType.Int).Value = transactionId;
                    if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Concurrent reconciliation detected.");
                }
                TransactionalAuditService.Record(cn, tx, AuthorizationService.CurrentUserName,
                    "BankReconciled", "BankStatementLine", lineId.ToString(),
                    (transfer ? "Transfer " : "Payment ") + transactionId);
                tx.Commit();
            }
        }
        /// <summary>Undo a reconciliation without deleting its historical audit trail.</summary>
        public static void Unmatch(int statementLineId, string reason)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (statementLineId <= 0) throw new ArgumentException("Invalid statement identifier.");
            if (String.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5 || reason.Length > 500)
                throw new ArgumentException("A reason of 5-500 characters is required.");
            using (var cn = Db.Open()) using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
            {
                string previous;
                using (var cmd = new SqlCommand(@"SELECT CASE WHEN MatchedPaymentId IS NOT NULL
THEN N'Payment:' + CONVERT(nvarchar(20),MatchedPaymentId)
WHEN MatchedTransferId IS NOT NULL THEN N'Transfer:' + CONVERT(nvarchar(20),MatchedTransferId)
ELSE NULL END FROM dbo.BankStatementLines WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id", cn, tx))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = statementLineId;
                    previous = cmd.ExecuteScalar() as string;
                }
                if (previous == null) throw new InvalidOperationException("Statement line is missing or not matched.");
                using (var cmd = new SqlCommand(@"UPDATE dbo.BankStatementLines SET MatchedPaymentId=NULL,
MatchedTransferId=NULL WHERE Id=@id AND (MatchedPaymentId IS NOT NULL OR MatchedTransferId IS NOT NULL)", cn, tx))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = statementLineId;
                    if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Concurrent reconciliation change detected.");
                }
                TransactionalAuditService.Record(cn, tx, AuthorizationService.CurrentUserName,
                    "BankReconciliationReversed", "BankStatementLine", statementLineId.ToString(),
                    previous + " | Reason: " + reason.Trim());
                tx.Commit();
            }
        }

        /// <summary>Account-specific totals for the selected statement date range.</summary>
        public static DataTable GetReconciliationSummary(int accountId, DateTime from, DateTime until)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0 || from.Date > until.Date) throw new ArgumentException("Invalid filter.");
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT COUNT(*) AS TotalLines,
SUM(CASE WHEN MatchedPaymentId IS NULL AND MatchedTransferId IS NULL THEN 1 ELSE 0 END) AS OpenLines,
COALESCE(SUM(CASE WHEN MatchedPaymentId IS NULL AND MatchedTransferId IS NULL THEN Amount ELSE 0 END),0) AS OpenNet,
COALESCE(SUM(CASE WHEN MatchedPaymentId IS NOT NULL OR MatchedTransferId IS NOT NULL THEN Amount ELSE 0 END),0) AS MatchedNet
FROM dbo.BankStatementLines WHERE CashBankAccountId=@account AND StatementDate>=@from AND StatementDate<=@until", cn))
            {
                cmd.Parameters.Add("@account", SqlDbType.Int).Value = accountId;
                cmd.Parameters.Add("@from", SqlDbType.Date).Value = from.Date;
                cmd.Parameters.Add("@until", SqlDbType.Date).Value = until.Date;
                var table = new DataTable(); using (var adapter = new SqlDataAdapter(cmd)) adapter.Fill(table);
                return table;
            }
        }

        /// <summary>Read-only candidates; never automatically posts a match.</summary>
        public static DataTable SuggestPayments(int statementLineId, int daysTolerance)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (statementLineId <= 0 || daysTolerance < 0 || daysTolerance > 30)
                throw new ArgumentException("Invalid statement line or date tolerance.");
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand(@"SELECT TOP (30) p.Id AS PaymentId,p.PaymentNo,p.PaymentDate,
CASE WHEN p.Direction=N'Receipt' THEN p.Amount ELSE -p.Amount END AS SignedAmount,
CASE WHEN p.ReferenceNo=l.ExternalReference THEN 1 ELSE 0 END AS ReferenceMatches,
ABS(DATEDIFF(day,CONVERT(date,p.PaymentDate),l.StatementDate)) AS DateDifferenceDays,
CASE WHEN p.ReferenceNo=l.ExternalReference THEN 90 ELSE 60 END
 - ABS(DATEDIFF(day,CONVERT(date,p.PaymentDate),l.StatementDate))*5 AS MatchScore
FROM dbo.BankStatementLines l
JOIN dbo.Payments p ON p.CashBankAccountId=l.CashBankAccountId
 AND (CASE WHEN p.Direction=N'Receipt' THEN p.Amount ELSE -p.Amount END)=l.Amount
LEFT JOIN dbo.BankStatementLines already ON already.MatchedPaymentId=p.Id
WHERE l.Id=@id AND l.MatchedPaymentId IS NULL AND l.MatchedTransferId IS NULL
 AND already.Id IS NULL AND ABS(DATEDIFF(day,CONVERT(date,p.PaymentDate),l.StatementDate))<=@days
ORDER BY CASE WHEN p.ReferenceNo=l.ExternalReference THEN 0 ELSE 1 END,
 ABS(DATEDIFF(day,CONVERT(date,p.PaymentDate),l.StatementDate)),p.Id", cn))
            {
                cmd.Parameters.Add("@id",SqlDbType.Int).Value=statementLineId;
                cmd.Parameters.Add("@days",SqlDbType.Int).Value=daysTolerance;
                var table=new DataTable();using(var adapter=new SqlDataAdapter(cmd))adapter.Fill(table);return table;
            }
        }

        public static DataTable GetStatement(int accountId, DateTime from, DateTime until)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0 || from.Date > until.Date) throw new ArgumentException("Invalid filter.");
            using (var cn = Db.Open()) using (var cmd = new SqlCommand(@"SELECT Id,StatementDate,ExternalReference,Description,Amount,
MatchedPaymentId,MatchedTransferId,
CASE WHEN MatchedPaymentId IS NULL AND MatchedTransferId IS NULL THEN N'Open' ELSE N'Matched' END AS Status
FROM dbo.BankStatementLines WHERE CashBankAccountId=@account AND StatementDate>=@from AND StatementDate<=@until
ORDER BY StatementDate DESC,Id DESC",cn))
            {
                cmd.Parameters.Add("@account",SqlDbType.Int).Value=accountId;
                cmd.Parameters.Add("@from",SqlDbType.Date).Value=from.Date;
                cmd.Parameters.Add("@until",SqlDbType.Date).Value=until.Date;
                var table = new DataTable(); using(var adapter=new SqlDataAdapter(cmd)) adapter.Fill(table); return table;
            }
        }

        /// <summary>Persist a bank risk snapshot for an account and exact analysis window.</summary>
        public static int SaveBankAlert(int accountId, DateTime fromDate, DateTime untilDateExclusive,
            string severity, long successfulImports, long failedImports, decimal failureRate, string recommendation)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (accountId <= 0 || fromDate >= untilDateExclusive || (untilDateExclusive-fromDate).TotalDays > 367)
                throw new ArgumentException("Invalid bank alert scope.");
            if (severity != "Critical" && severity != "Warning" && severity != "Information")
                throw new ArgumentException("Invalid severity.");
            if (successfulImports < 0 || failedImports <= 0 || failureRate < 0 || failureRate > 100)
                throw new ArgumentException("Invalid bank alert metrics.");
            if (recommendation == null || recommendation.Length > 500) throw new ArgumentException("Invalid recommendation.");
            using (var cn=Db.Open())
            using (var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                const string find = @"SELECT Id FROM dbo.BankImportAlerts WITH (UPDLOCK,HOLDLOCK)
WHERE CashBankAccountId=@account AND PeriodFrom=@from AND PeriodUntil=@until AND SeverityCode=@severity";
                int id; bool notifyCritical = false;
                using(var cmd=new SqlCommand(find,cn,tx))
                {
                    cmd.Parameters.Add("@account",SqlDbType.Int).Value=accountId;
                    cmd.Parameters.Add("@from",SqlDbType.Date).Value=fromDate.Date;
                    cmd.Parameters.Add("@until",SqlDbType.Date).Value=untilDateExclusive.Date;
                    cmd.Parameters.Add("@severity",SqlDbType.NVarChar,20).Value=severity;
                    object value=cmd.ExecuteScalar();
                    if(value==null)
                    {
                        notifyCritical = severity == "Critical";
                        using(var add=new SqlCommand(@"INSERT dbo.BankImportAlerts
(CashBankAccountId,PeriodFrom,PeriodUntil,SeverityCode,SuccessfulImports,FailedImports,FailureRate,Recommendation,StatusCode,FirstDetectedAt,LastDetectedAt)
VALUES(@account,@from,@until,@severity,@ok,@bad,@rate,@recommendation,N'New',SYSUTCDATETIME(),SYSUTCDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS int);",cn,tx))
                        {
                            AddAlertParameters(add,accountId,fromDate,untilDateExclusive,severity,successfulImports,failedImports,failureRate,recommendation);
                            id=Convert.ToInt32(add.ExecuteScalar());
                        }
                    }
                    else
                    {
                        id=Convert.ToInt32(value);
                        using(var state=new SqlCommand("SELECT StatusCode FROM dbo.BankImportAlerts WITH(UPDLOCK,HOLDLOCK) WHERE Id=@id",cn,tx))
                        { state.Parameters.Add("@id",SqlDbType.Int).Value=id; notifyCritical = severity == "Critical" && Convert.ToString(state.ExecuteScalar()) == "Resolved"; }
                        using(var update=new SqlCommand(@"UPDATE dbo.BankImportAlerts SET SuccessfulImports=@ok,FailedImports=@bad,FailureRate=@rate,
Recommendation=@recommendation,LastDetectedAt=SYSUTCDATETIME(),
StatusCode=CASE WHEN StatusCode=N'Resolved' THEN N'New' ELSE StatusCode END,ResolvedAt=NULL,ResolvedBy=NULL
WHERE Id=@id",cn,tx))
                        {
                            AddMetricParameters(update,successfulImports,failedImports,failureRate,recommendation);
                            update.Parameters.Add("@id",SqlDbType.Int).Value=id; update.ExecuteNonQuery();
                        }
                    }
                }
                if (notifyCritical)
                {
                    using(var enqueue=new SqlCommand(@"INSERT dbo.BankAlertNotificationOutbox(AlertId,EventKey,EventType)
VALUES(@alert,CONVERT(nvarchar(20),@alert)+N':'+CONVERT(nvarchar(36),NEWID()),N'CriticalAlertCreated');",cn,tx))
                    { enqueue.Parameters.Add("@alert",SqlDbType.Int).Value=id; enqueue.ExecuteNonQuery(); }
                }
                tx.Commit(); return id;
            }
        }

        private static void AddAlertParameters(SqlCommand cmd,int accountId,DateTime from,DateTime until,string severity,long ok,long bad,decimal rate,string recommendation)
        {
            cmd.Parameters.Add("@account",SqlDbType.Int).Value=accountId;
            cmd.Parameters.Add("@from",SqlDbType.Date).Value=from.Date;
            cmd.Parameters.Add("@until",SqlDbType.Date).Value=until.Date;
            cmd.Parameters.Add("@severity",SqlDbType.NVarChar,20).Value=severity;
            AddMetricParameters(cmd,ok,bad,rate,recommendation);
        }
        private static void AddMetricParameters(SqlCommand cmd,long ok,long bad,decimal rate,string recommendation)
        {
            cmd.Parameters.Add("@ok",SqlDbType.BigInt).Value=ok;
            cmd.Parameters.Add("@bad",SqlDbType.BigInt).Value=bad;
            var p=cmd.Parameters.Add("@rate",SqlDbType.Decimal);p.Precision=7;p.Scale=2;p.Value=rate;
            cmd.Parameters.Add("@recommendation",SqlDbType.NVarChar,500).Value=recommendation;
        }

        public static DataTable GetBankAlerts(DateTime fromDate,DateTime untilDateExclusive)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if(fromDate>=untilDateExclusive || (untilDateExclusive-fromDate).TotalDays>367) throw new ArgumentException("Invalid date range.");
            using(var cn=Db.Open()) using(var cmd=new SqlCommand(@"SELECT x.Id,x.CashBankAccountId,a.Code+' - '+a.Name AS Account,
 x.PeriodFrom,x.PeriodUntil,x.SeverityCode,x.SuccessfulImports,x.FailedImports,x.FailureRate,x.Recommendation,x.StatusCode,
 x.FirstDetectedAt,x.LastDetectedAt,x.ResolvedAt,x.ResolvedBy,x.AssignedToUserId,
 u.DisplayName AS AssignedTo,x.DueAtUtc,x.PriorityCode,
 CASE WHEN x.DueAtUtc IS NOT NULL AND x.DueAtUtc<SYSUTCDATETIME() AND x.StatusCode<>N'Resolved' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsOverdue
FROM dbo.BankImportAlerts x INNER JOIN dbo.CashBankAccounts a ON a.Id=x.CashBankAccountId
LEFT JOIN dbo.AppUsers u ON u.UserId=x.AssignedToUserId
WHERE x.PeriodFrom>=@from AND x.PeriodFrom<@until ORDER BY CASE x.StatusCode WHEN N'New' THEN 0 WHEN N'InProgress' THEN 1 ELSE 2 END,
 CASE x.SeverityCode WHEN N'Critical' THEN 0 WHEN N'Warning' THEN 1 ELSE 2 END,x.LastDetectedAt DESC",cn))
            {
                cmd.Parameters.Add("@from",SqlDbType.Date).Value=fromDate.Date;
                cmd.Parameters.Add("@until",SqlDbType.Date).Value=untilDateExclusive.Date;
                var dt=new DataTable();using(var da=new SqlDataAdapter(cmd))da.Fill(dt);return dt;
            }
        }

        public static DataTable GetBankAlertDashboard(DateTime fromDate, DateTime untilDateExclusive)
        {
            return GetBankAlertDashboard(fromDate, untilDateExclusive, null, null);
        }

        /// <summary>Dashboard metrics calculated over one consistently filtered alert population.</summary>
        public static DataTable GetBankAlertDashboard(DateTime fromDate, DateTime untilDateExclusive, int? cashBankAccountId, int? assignedToUserId)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if (fromDate >= untilDateExclusive || (untilDateExclusive - fromDate).TotalDays > 367)
                throw new ArgumentException("Invalid date range.");
            const string sql = @"
;WITH FilteredAlerts AS
(
    SELECT * FROM dbo.BankImportAlerts
    WHERE PeriodFrom>=@from AND PeriodFrom<@until
      AND (@accountId IS NULL OR CashBankAccountId=@accountId)
      AND (@assigneeId IS NULL OR AssignedToUserId=@assigneeId)
)
SELECT N'Status' AS Category, StatusCode AS Item, COUNT_BIG(*) AS AlertCount
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
GROUP BY StatusCode
UNION ALL
SELECT N'Severity', SeverityCode, COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
GROUP BY SeverityCode
UNION ALL
SELECT N'Priority', PriorityCode, COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
GROUP BY PriorityCode
UNION ALL
SELECT N'Assignment', CASE WHEN AssignedToUserId IS NULL THEN N'Unassigned' ELSE N'Assigned' END, COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until AND StatusCode<>N'Resolved'
GROUP BY CASE WHEN AssignedToUserId IS NULL THEN N'Unassigned' ELSE N'Assigned' END
UNION ALL
SELECT N'SLA', CASE WHEN DueAtUtc IS NULL THEN N'NoDueDate' WHEN DueAtUtc<SYSUTCDATETIME() THEN N'Overdue' ELSE N'OnTime' END, COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until AND StatusCode<>N'Resolved'
GROUP BY CASE WHEN DueAtUtc IS NULL THEN N'NoDueDate' WHEN DueAtUtc<SYSUTCDATETIME() THEN N'Overdue' ELSE N'OnTime' END
UNION ALL
SELECT N'Resolution', N'Resolved', COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until AND StatusCode=N'Resolved'
UNION ALL
SELECT N'Resolution', N'Open', COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until AND StatusCode<>N'Resolved'
UNION ALL
SELECT N'MonthlyTrend', CONVERT(nvarchar(7), PeriodFrom, 126), COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
GROUP BY CONVERT(nvarchar(7), PeriodFrom, 126)
UNION ALL
SELECT N'AssigneeWorkload', COALESCE(u.DisplayName,N'Unassigned'), COUNT_BIG(*)
FROM FilteredAlerts x LEFT JOIN dbo.AppUsers u ON u.UserId=x.AssignedToUserId
WHERE x.PeriodFrom>=@from AND x.PeriodFrom<@until AND x.StatusCode<>N'Resolved'
GROUP BY COALESCE(u.DisplayName,N'Unassigned')
UNION ALL
SELECT N'ExecutivePriority', N'CriticalOpen', COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
 AND StatusCode<>N'Resolved' AND SeverityCode=N'Critical'
UNION ALL
SELECT N'ExecutivePriority', N'CriticalOverdue', COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
 AND StatusCode<>N'Resolved' AND SeverityCode=N'Critical'
 AND DueAtUtc IS NOT NULL AND DueAtUtc<SYSUTCDATETIME()
UNION ALL
SELECT N'ExecutivePriority', N'DueWithin24Hours', COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
 AND StatusCode<>N'Resolved' AND DueAtUtc>=SYSUTCDATETIME()
 AND DueAtUtc<DATEADD(HOUR,24,SYSUTCDATETIME())
UNION ALL
SELECT N'ExecutivePriority', N'UnassignedCritical', COUNT_BIG(*)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
 AND StatusCode<>N'Resolved' AND SeverityCode=N'Critical' AND AssignedToUserId IS NULL
UNION ALL
SELECT N'Rate', N'ResolutionPercent',
       CASE WHEN COUNT_BIG(*)=0 THEN 0 ELSE CONVERT(bigint,100.0*SUM(CASE WHEN StatusCode=N'Resolved' THEN 1 ELSE 0 END)/COUNT_BIG(*)) END
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
UNION ALL
SELECT N'Rate', N'OverdueOpenPercent',
       CASE WHEN COUNT_BIG(*)=0 THEN 0 ELSE CONVERT(bigint,100.0*SUM(CASE WHEN DueAtUtc<SYSUTCDATETIME() THEN 1 ELSE 0 END)/COUNT_BIG(*)) END
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
 AND StatusCode<>N'Resolved' AND DueAtUtc IS NOT NULL
UNION ALL
SELECT N'Rate', N'AssignedOpenPercent',
       CASE WHEN COUNT_BIG(*)=0 THEN 0 ELSE CONVERT(bigint,100.0*SUM(CASE WHEN AssignedToUserId IS NOT NULL THEN 1 ELSE 0 END)/COUNT_BIG(*)) END
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
 AND StatusCode<>N'Resolved'
UNION ALL
SELECT N'AverageResolutionMinutes', N'AllResolved',
       COALESCE(CONVERT(bigint,AVG(CONVERT(decimal(18,2),DATEDIFF_BIG(MINUTE,FirstDetectedAt,ResolvedAt)))),0)
FROM FilteredAlerts WHERE PeriodFrom>=@from AND PeriodFrom<@until
 AND StatusCode=N'Resolved' AND ResolvedAt IS NOT NULL AND ResolvedAt>=FirstDetectedAt
UNION ALL
SELECT N'AverageResolutionMinutes', COALESCE(u.DisplayName,N'Unassigned'),
       COALESCE(CONVERT(bigint,AVG(CONVERT(decimal(18,2),DATEDIFF_BIG(MINUTE,x.FirstDetectedAt,x.ResolvedAt)))),0)
FROM FilteredAlerts x LEFT JOIN dbo.AppUsers u ON u.UserId=x.AssignedToUserId
WHERE x.PeriodFrom>=@from AND x.PeriodFrom<@until AND x.StatusCode=N'Resolved'
 AND x.ResolvedAt IS NOT NULL AND x.ResolvedAt>=x.FirstDetectedAt
GROUP BY COALESCE(u.DisplayName,N'Unassigned')
ORDER BY Category, Item;";
            using (var cn = Db.Open()) using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@from", SqlDbType.Date).Value = fromDate.Date;
                cmd.Parameters.Add("@until", SqlDbType.Date).Value = untilDateExclusive.Date;
                cmd.Parameters.Add("@accountId", SqlDbType.Int).Value = (object)cashBankAccountId ?? DBNull.Value;
                cmd.Parameters.Add("@assigneeId", SqlDbType.Int).Value = (object)assignedToUserId ?? DBNull.Value;
                var result = new DataTable();
                using (var adapter = new SqlDataAdapter(cmd)) adapter.Fill(result);
                return result;
            }
        }

        public static DataTable GetBankAlertAccounts()
        {
            AuthorizationService.RequireActiveSession("cashbank");
            using (var cn = Db.Open()) using (var da = new SqlDataAdapter("SELECT Id,Code+' - '+Name AS Name FROM dbo.CashBankAccounts WHERE IsActive=1 AND CurrencyCode='IRR' ORDER BY Name", cn))
            { var dt = new DataTable(); da.Fill(dt); return dt; }
        }

        public static DataTable GetBankAlertAssignees()
        {
            AuthorizationService.RequireActiveSession("cashbank");
            using(var cn=Db.Open()) using(var da=new SqlDataAdapter("SELECT UserId,DisplayName,UserName,RoleName FROM dbo.AppUsers WHERE IsActive=1 AND (LockedUntil IS NULL OR LockedUntil<=SYSUTCDATETIME()) ORDER BY DisplayName,UserName",cn))
            { var dt=new DataTable(); da.Fill(dt); return dt; }
        }

        public static void AssignBankAlert(int alertId,int? userId)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if(alertId<=0) throw new ArgumentOutOfRangeException(nameof(alertId));
            using(var cn=Db.Open()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                object old; using(var q=new SqlCommand("SELECT AssignedToUserId FROM dbo.BankImportAlerts WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id",cn,tx))
                { q.Parameters.Add("@id",SqlDbType.Int).Value=alertId; old=q.ExecuteScalar(); if(old==null) throw new InvalidOperationException("Bank alert was not found."); }
                if(userId.HasValue) using(var check=new SqlCommand("SELECT COUNT(*) FROM dbo.AppUsers WHERE UserId=@u AND IsActive=1 AND (LockedUntil IS NULL OR LockedUntil<=SYSUTCDATETIME())",cn,tx))
                { check.Parameters.Add("@u",SqlDbType.Int).Value=userId.Value; if(Convert.ToInt32(check.ExecuteScalar())!=1) throw new InvalidOperationException("Assignee is not an active user."); }
                using(var update=new SqlCommand("UPDATE dbo.BankImportAlerts SET AssignedToUserId=@u WHERE Id=@id",cn,tx))
                { update.Parameters.Add("@u",SqlDbType.Int).Value=(object)userId??DBNull.Value; update.Parameters.Add("@id",SqlDbType.Int).Value=alertId; update.ExecuteNonQuery(); }
                RecordAlertHistory(cn,tx,alertId,"Assignment",old==DBNull.Value?null:Convert.ToString(old),userId.HasValue?userId.Value.ToString():null); tx.Commit();
            }
        }

        public static void SetBankAlertSchedule(int alertId,DateTime? dueAtUtc,string priority)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if(alertId<=0 || (priority!="Low" && priority!="Normal" && priority!="High" && priority!="Urgent")) throw new ArgumentException("Invalid alert schedule or priority.");
            using(var cn=Db.Open()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                object oldDue,oldPriority; using(var q=new SqlCommand("SELECT DueAtUtc,PriorityCode FROM dbo.BankImportAlerts WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id",cn,tx))
                { q.Parameters.Add("@id",SqlDbType.Int).Value=alertId; using(var r=q.ExecuteReader()){if(!r.Read()) throw new InvalidOperationException("Bank alert was not found.");oldDue=r.IsDBNull(0)?null:(object)r.GetDateTime(0).ToString("o");oldPriority=r.GetString(1);}}
                using(var update=new SqlCommand("UPDATE dbo.BankImportAlerts SET DueAtUtc=@due,PriorityCode=@priority WHERE Id=@id",cn,tx))
                { update.Parameters.Add("@due",SqlDbType.DateTime2).Value=(object)dueAtUtc??DBNull.Value; update.Parameters.Add("@priority",SqlDbType.NVarChar,20).Value=priority; update.Parameters.Add("@id",SqlDbType.Int).Value=alertId; update.ExecuteNonQuery(); }
                string newDue=dueAtUtc.HasValue?dueAtUtc.Value.ToString("o"):null;
                if(Convert.ToString(oldDue)!=Convert.ToString(newDue)) RecordAlertHistory(cn,tx,alertId,"DueDate",Convert.ToString(oldDue),newDue);
                if(Convert.ToString(oldPriority)!=priority) RecordAlertHistory(cn,tx,alertId,"Priority",Convert.ToString(oldPriority),priority);
                tx.Commit();
            }
        }

        public static void AddBankAlertNote(int alertId,string note)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if(alertId<=0 || String.IsNullOrWhiteSpace(note) || note.Trim().Length>1000) throw new ArgumentException("Note must contain 1-1000 characters.");
            using(var cn=Db.Open()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                using(var exists=new SqlCommand("SELECT COUNT(*) FROM dbo.BankImportAlerts WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id",cn,tx))
                { exists.Parameters.Add("@id",SqlDbType.Int).Value=alertId; if(Convert.ToInt32(exists.ExecuteScalar())!=1) throw new InvalidOperationException("Bank alert was not found."); }
                using(var user=new SqlCommand("SELECT UserId FROM dbo.AppUsers WHERE UserName=@name AND IsActive=1",cn,tx))
                { user.Parameters.Add("@name",SqlDbType.NVarChar,80).Value=AuthorizationService.CurrentUserName; object uid=user.ExecuteScalar(); if(uid==null) throw new UnauthorizedAccessException("A SQL-backed user account is required to add notes.");
                    using(var add=new SqlCommand("INSERT dbo.BankImportAlertNotes(AlertId,NoteText,CreatedByUserId) VALUES(@a,@n,@u)",cn,tx))
                    { add.Parameters.Add("@a",SqlDbType.Int).Value=alertId; add.Parameters.Add("@n",SqlDbType.NVarChar,1000).Value=note.Trim(); add.Parameters.Add("@u",SqlDbType.Int).Value=uid; add.ExecuteNonQuery(); }
                }
                tx.Commit();
            }
        }

        public static DataTable GetBankAlertNotes(int alertId)
        {
            AuthorizationService.RequireActiveSession("cashbank"); if(alertId<=0) throw new ArgumentOutOfRangeException(nameof(alertId));
            using(var cn=Db.Open()) using(var cmd=new SqlCommand("SELECT n.NoteText,n.CreatedAtUtc,u.DisplayName,u.UserName FROM dbo.BankImportAlertNotes n INNER JOIN dbo.AppUsers u ON u.UserId=n.CreatedByUserId WHERE n.AlertId=@a ORDER BY n.CreatedAtUtc DESC,n.Id DESC",cn))
            { cmd.Parameters.Add("@a",SqlDbType.Int).Value=alertId; var dt=new DataTable(); using(var da=new SqlDataAdapter(cmd)) da.Fill(dt); return dt; }
        }

        public static DataTable GetBankAlertHistory(int alertId)
        {
            AuthorizationService.RequireActiveSession("cashbank"); if(alertId<=0) throw new ArgumentOutOfRangeException(nameof(alertId));
            using(var cn=Db.Open()) using(var cmd=new SqlCommand("SELECT h.ChangeType,h.OldValue,h.NewValue,h.ChangedAtUtc,u.DisplayName,u.UserName FROM dbo.BankImportAlertHistory h INNER JOIN dbo.AppUsers u ON u.UserId=h.ChangedByUserId WHERE h.AlertId=@a ORDER BY h.ChangedAtUtc DESC,h.Id DESC",cn))
            { cmd.Parameters.Add("@a",SqlDbType.Int).Value=alertId; var dt=new DataTable(); using(var da=new SqlDataAdapter(cmd)) da.Fill(dt); return dt; }
        }

        private static void RecordAlertHistory(SqlConnection cn,SqlTransaction tx,int alertId,string type,string oldValue,string newValue)
        {
            using(var cmd=new SqlCommand(@"INSERT dbo.BankImportAlertHistory(AlertId,ChangedByUserId,ChangeType,OldValue,NewValue)
SELECT @a,UserId,@type,@old,@new FROM dbo.AppUsers WHERE UserName=@name AND IsActive=1",cn,tx))
            { cmd.Parameters.Add("@a",SqlDbType.Int).Value=alertId; cmd.Parameters.Add("@type",SqlDbType.NVarChar,30).Value=type; cmd.Parameters.Add("@old",SqlDbType.NVarChar,200).Value=(object)oldValue??DBNull.Value; cmd.Parameters.Add("@new",SqlDbType.NVarChar,200).Value=(object)newValue??DBNull.Value; cmd.Parameters.Add("@name",SqlDbType.NVarChar,80).Value=AuthorizationService.CurrentUserName; if(cmd.ExecuteNonQuery()!=1) throw new UnauthorizedAccessException("A SQL-backed user account is required for workflow history."); }
        }

        public static void UpdateBankAlertStatus(int alertId,string status)
        {
            AuthorizationService.RequireActiveSession("cashbank");
            if(alertId<=0 || (status!="New" && status!="InProgress" && status!="Resolved")) throw new ArgumentException("Invalid alert status.");
            using(var cn=Db.Open()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                string old; using(var q=new SqlCommand("SELECT StatusCode FROM dbo.BankImportAlerts WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id",cn,tx))
                { q.Parameters.Add("@id",SqlDbType.Int).Value=alertId; old=q.ExecuteScalar() as string; if(old==null) throw new InvalidOperationException("Bank alert was not found."); }
                using(var cmd=new SqlCommand(@"UPDATE dbo.BankImportAlerts SET StatusCode=@status,
ResolvedAt=CASE WHEN @status=N'Resolved' THEN SYSUTCDATETIME() ELSE NULL END,
ResolvedBy=CASE WHEN @status=N'Resolved' THEN @user ELSE NULL END WHERE Id=@id",cn,tx))
                { cmd.Parameters.Add("@status",SqlDbType.NVarChar,20).Value=status; cmd.Parameters.Add("@user",SqlDbType.NVarChar,100).Value=AuthorizationService.CurrentUserName; cmd.Parameters.Add("@id",SqlDbType.Int).Value=alertId; cmd.ExecuteNonQuery(); }
                if(old!=status) RecordAlertHistory(cn,tx,alertId,"Status",old,status); tx.Commit();
            }
        }
    }
}
