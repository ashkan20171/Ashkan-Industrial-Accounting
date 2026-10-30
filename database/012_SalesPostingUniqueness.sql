-- Stage 35: review duplicates and back up the database before running.
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID(N'dbo.JournalEntries', N'U') IS NULL
  THROW 51000, 'JournalEntries missing', 1;
 IF EXISTS (SELECT ReferenceNo FROM dbo.JournalEntries WITH (TABLOCKX,HOLDLOCK)
            WHERE ReferenceNo IS NOT NULL GROUP BY ReferenceNo HAVING COUNT(*) > 1)
  THROW 51001, 'Duplicate journal references exist. Resolve before migration.', 1;
 IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.JournalEntries')
                AND name=N'UX_JournalEntries_ReferenceNo')
  CREATE UNIQUE INDEX UX_JournalEntries_ReferenceNo ON dbo.JournalEntries(ReferenceNo)
   WHERE ReferenceNo IS NOT NULL;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
