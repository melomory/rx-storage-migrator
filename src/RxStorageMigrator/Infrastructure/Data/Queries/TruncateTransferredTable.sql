IF OBJECT_ID(N'[dbo].[Converter_DocVer_Transferred]', N'U') IS NOT NULL
BEGIN
    TRUNCATE TABLE [dbo].[Converter_DocVer_Transferred];
END