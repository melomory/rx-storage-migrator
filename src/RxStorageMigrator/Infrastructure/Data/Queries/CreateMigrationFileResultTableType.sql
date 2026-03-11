IF NOT EXISTS
(
    SELECT 1
    FROM sys.types t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.is_table_type = 1
      AND t.name = 'MigrationFileResultTableType'
      AND s.name = 'dbo'
)
BEGIN
CREATE TYPE dbo.MigrationFileResultTableType AS TABLE
  (
  RowId bigint NOT NULL PRIMARY KEY, -- Id из MigrationQueue
  XRecId int NOT NULL,
  Number int NOT NULL,
  RxDocId bigint NOT NULL,
  BodyId UNIQUEIDENTIFIER NOT NULL,
  MigrationStatus nvarchar(50) NOT NULL,
  Comment nvarchar(MAX) NULL,
  D5FilePath nvarchar(MAX) NULL,
  RxFilePath nvarchar(MAX) NULL
  );
END
