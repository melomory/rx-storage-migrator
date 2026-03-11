CREATE TABLE dbo.Converter_Documents_EDocVersion_Current
(
    Id bigint NOT NULL,
    XRecID bigint NOT NULL,
    XRecID_Public bigint NULL,
    Body_Id uniqueidentifier NULL,
    Storage bigint NOT NULL,
    PublicBody_Id uniqueidentifier NULL,
    PublicBody_Storage bigint NULL,
    RXDocId bigint NOT NULL
);
GO

CREATE TABLE dbo.Converter_DocVer_Transferred
(
    Id bigint IDENTITY(1,1) NOT NULL,
    XRecID bigint NOT NULL,
    Number bigint NOT NULL,
    RXDocId bigint NOT NULL,
    BodyId nvarchar(50) NOT NULL,
    MigrStatus nvarchar(50) NOT NULL,
    Comment nvarchar(max) NULL,
    D5FilePath nvarchar(max) NULL,
    RXFilePath nvarchar(max) NULL,
    CONSTRAINT PK_Converter_DocVer_Transferred PRIMARY KEY CLUSTERED
    (
        XRecID,
        BodyId
    )
    WITH
    (
        PAD_INDEX = OFF,
        STATISTICS_NORECOMPUTE = OFF,
        IGNORE_DUP_KEY = OFF,
        ALLOW_ROW_LOCKS = ON,
        ALLOW_PAGE_LOCKS = ON
    ),
    CONSTRAINT UX_DocVer UNIQUE NONCLUSTERED
    (
        RXDocId,
        BodyId
    )
);
GO

CREATE TABLE dbo.Converter_Documents_Sorted
(
    Id bigint IDENTITY(1,1) NOT NULL,
    XRecID bigint NOT NULL, -- ИД документа в Directum 5
    CONSTRAINT PK_Converter_Documents_Sorted PRIMARY KEY CLUSTERED
    (
        XRecID
    )
    WITH
    (
        PAD_INDEX = OFF,
        STATISTICS_NORECOMPUTE = OFF,
        IGNORE_DUP_KEY = OFF,
        ALLOW_ROW_LOCKS = ON,
        ALLOW_PAGE_LOCKS = ON
    )
);
GO

CREATE TABLE dbo.Converter_Documents_StorageD5
(
    Id bigint NOT NULL,
    Name varchar(512) NULL,
    StorageType varchar(1) NOT NULL, -- F - File, S - SQL
    FSPath varchar(max) NULL         -- Для типа F указывается путь к файловому хранилищу.
                                     -- Для типа S поле остается пустым.
);
GO

CREATE TABLE dbo.Converter_Documents_StorageRX
(
    Id bigint NOT NULL,
    Name varchar(512) NULL,
    FSPath varchar(max) NULL         -- Путь к файловому хранилищу целевой системы
);
GO
