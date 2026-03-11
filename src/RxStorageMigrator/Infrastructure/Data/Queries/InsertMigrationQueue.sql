BEGIN TRY

INSERT INTO Converter_DocVer_MigrationQueue (D5DocId, VersionNumber, BodyId, D5VerId, Extension, D5StorageId,
                                                 RxDocId, RxStorageId, SortId)
SELECT ver.EDocID    AS D5DocId,
       ver.Number    AS VersionNumber,
       tmp.Body_Id   AS BodyId,
       ver.XRecID    AS D5VerId,
       editor.AppExt AS Extension,
       doc.Storage   AS D5StorageId,
       tmp.RXDocId   AS RxDocId,
       tmp.Storage   AS RxStorageId,
       sort.Id       AS SortId
  FROM SBEDocVer AS ver
  JOIN SBEDoc AS doc
    ON doc.XRecID = ver.EDocID
  JOIN (SELECT ver1_rx.Id      AS Id,
               ver1_rx.XRecID  AS XRecID,
               ver1_rx.Body_Id AS Body_Id,
               ver1_rx.Storage AS Storage,
               ver1_rx.RXDocId AS RXDocId
          FROM Converter_Documents_EDocVersion_Current AS ver1_rx
         WHERE ver1_rx.Body_Id IS NOT NULL -- вывести список обычных версий

         UNION ALL

        SELECT ver2_rx.Id                 AS Id,
               ver2_rx.XRecID_Public      AS XRecID,
               ver2_rx.PublicBody_Id      AS Body_Id,
               ver2_rx.PublicBody_Storage AS Storage,
               ver2_rx.RXDocId            AS RXDocId
          FROM Converter_Documents_EDocVersion_Current AS ver2_rx
         WHERE ver2_rx.PublicBody_Id IS NOT NULL -- вывести список public версий
  ) AS tmp
    ON ver.XRecID = tmp.XRecID
  JOIN MBAnalit AS editor
    ON ver.Editor = editor.Analit
  JOIN Converter_Documents_Sorted AS sort
    ON doc.XRecID = sort.XRecID
 ORDER BY sort.Id ASC, ver.XRecID ASC, ver.EDocID ASC;


END TRY
BEGIN CATCH
IF ERROR_NUMBER() NOT IN (2601, 2627)
        THROW;
END CATCH
