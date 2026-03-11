  WITH cte AS
         (SELECT TOP(@BatchSize)
                 Id AS RowId,
                 D5DocId,
                 VersionNumber,
                 BodyId,
                 D5VerId,
                 Extension,
                 D5StorageId,
                 RxDocId,
                 RxStorageId,
                 SortId,
                 Status,
                 StartedAt
            FROM Converter_DocVer_MigrationQueue
  WITH (ROWLOCK, READPAST, UPDLOCK)
 WHERE Status IS NULL
 ORDER BY SortId ASC
   )
  UPDATE cte
     SET Status    = @MigrationStatus,
         StartedAt = SYSUTCDATETIME()
  OUTPUT
    inserted.RowId,
    inserted.D5DocId,
    inserted.VersionNumber,
    inserted.BodyId,
    inserted.D5VerId,
    inserted.Extension,
    inserted.D5StorageId,
    inserted.RxDocId,
    inserted.RxStorageId;
