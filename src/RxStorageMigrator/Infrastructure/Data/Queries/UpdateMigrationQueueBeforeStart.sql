UPDATE Converter_DocVer_MigrationQueue
   SET Status = NULL
 WHERE Status = 'InProgress' OR Status = 'Error';
