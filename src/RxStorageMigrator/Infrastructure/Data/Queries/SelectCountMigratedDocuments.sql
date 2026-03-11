SELECT COUNT(ver.XRecId)
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
 WHERE NOT EXISTS (SELECT 1
                     FROM Converter_DocVer_Transferred AS tr
                    WHERE tr.XRecID = doc.XRecID
                      AND tr.Number = ver.Number
                      AND tr.MigrStatus = N'Performed');
