IF OBJECT_ID('dbo.SaveMigrationFileResults') IS NOT NULL
    DROP PROCEDURE dbo.SaveMigrationFileResults;

EXEC('
CREATE PROCEDURE dbo.SaveMigrationFileResults
  @Items dbo.MigrationFileResultTableType READONLY
  AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

BEGIN TRAN;
    ----------------------------------------------------------------
    -- 1 Обновить очередь
    ----------------------------------------------------------------
UPDATE q
   SET q.Status = i.MigrationStatus,
       q.CompletedAt = SYSUTCDATETIME()
     FROM Converter_DocVer_MigrationQueue AS q
        JOIN @Items AS i
    ON q.Id = i.RowId;

----------------------------------------------------------------
-- 2 UPDATE существующих (по PK: XRecID + BodyId)
----------------------------------------------------------------
UPDATE t WITH (UPDLOCK, SERIALIZABLE)
   SET
     t.Number = i.Number,
     t.RXDocId = i.RXDocId,
     t.MigrStatus = i.MigrationStatus,
     t.Comment = i.Comment,
     t.D5FilePath = i.D5FilePath,
     t.RXFilePath = i.RXFilePath
  FROM dbo.Converter_DocVer_Transferred AS t
    JOIN @Items AS i
    ON t.XRecID = i.XRecID
      AND t.BodyId = i.BodyId;

----------------------------------------------------------------
-- 3 INSERT отсутствующих (защита от гонки)
----------------------------------------------------------------
INSERT INTO Converter_DocVer_Transferred
(
  XRecID,
  Number,
  RXDocId,
  BodyId,
  MigrStatus,
  Comment,
  D5FilePath,
  RXFilePath
)
SELECT
  i.XRecID,
  i.Number,
  i.RXDocId,
  i.BodyId,
  i.MigrationStatus,
  i.Comment,
  i.D5FilePath,
  i.RXFilePath
  FROM @Items AS i
 WHERE NOT EXISTS
         (
           SELECT 1
             FROM dbo.Converter_DocVer_Transferred AS t WITH (UPDLOCK, SERIALIZABLE)
           WHERE t.XRecID = i.XRecID
           AND t.BodyId = i.BodyId
         );

COMMIT;
END
');
