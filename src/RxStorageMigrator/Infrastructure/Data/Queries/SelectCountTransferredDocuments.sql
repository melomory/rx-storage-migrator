SELECT SUM(CASE WHEN tr.MigrStatus = 'Performed' THEN 1 ELSE 0 END) AS PerformedCount,
       COUNT(*)                                                     AS TotalCount
  FROM Converter_DocVer_Transferred AS tr;
