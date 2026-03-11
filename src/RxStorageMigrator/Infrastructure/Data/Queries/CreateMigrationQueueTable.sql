IF OBJECT_ID('Converter_DocVer_MigrationQueue') IS NULL
BEGIN
CREATE TABLE Converter_DocVer_MigrationQueue
(
  Id            BIGINT IDENTITY PRIMARY KEY,
  D5DocId       INT              NOT NULL,
  VersionNumber INT              NOT NULL,
  BodyId        UNIQUEIDENTIFIER NOT NULL,
  D5VerId       INT              NOT NULL,
  Extension     NVARCHAR(50)     NOT NULL,
  D5StorageId   BIGINT           NOT NULL,
  RxDocId       BIGINT           NOT NULL,
  RxStorageId   BIGINT           NOT NULL,
  SortId        BIGINT           NOT NULL,

  Status        NVARCHAR(50)     NULL, -- InProgress, Performed, Error
  CreatedAt     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
  StartedAt     DATETIME2 NULL,
  CompletedAt   DATETIME2 NULL,

  CONSTRAINT UX_MigrationQueue UNIQUE (RXDocId, BodyId)
);

CREATE INDEX IX_MigrationQueue_Status_Id
  ON Converter_DocVer_MigrationQueue (Status, Id);
END
