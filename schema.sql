CREATE TABLE [StatusDictionary]
(
    [Id]           INT           IDENTITY(1,1) NOT NULL,
    [Code]         INT           NOT NULL,
    [Name]         NVARCHAR(100) NOT NULL,
    [Description]  NVARCHAR(500) NULL,
    [ArchiveLevel] INT           NOT NULL,
    CONSTRAINT [PK_StatusDictionary] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_StatusDictionary_Code] UNIQUE ([Code])
);

CREATE TABLE [Territories]
(
    [Id]            INT           IDENTITY(1,1) NOT NULL,
    [TerritoryCode] NVARCHAR(10)  NOT NULL,
    [TerritoryName] NVARCHAR(255) NOT NULL,
    [ArchiveLevel]  INT           NOT NULL,
    [CreateTime]    DATETIME2     NOT NULL,
    [ChangeTime]    DATETIME2     NULL,
    [ArchiveTime]   DATETIME2     NULL,
    CONSTRAINT [PK_Territories] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Territories_TerritoryCode] UNIQUE ([TerritoryCode])
);

CREATE TABLE [CatalogProductTypes]
(
    [Id]           INT           IDENTITY(1,1) NOT NULL,
    [Code]         NVARCHAR(50)  NOT NULL,
    [Description]  NVARCHAR(500) NOT NULL,
    [ArchiveLevel] INT           NOT NULL,
    [CreateTime]   DATETIME2     NOT NULL,
    [ChangeTime]   DATETIME2     NULL,
    [ArchiveTime]  DATETIME2     NULL,
    CONSTRAINT [PK_CatalogProductTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_CatalogProductTypes_Code] UNIQUE ([Code])
);

CREATE TABLE [Companies]
(
    [Id]            INT           IDENTITY(1,1) NOT NULL,
    [CompanyCode]   NVARCHAR(50)  NOT NULL,
    [LegalName]     NVARCHAR(255) NOT NULL,
    [ShortName]     NVARCHAR(100) NOT NULL,
    [Inn]           NVARCHAR(12)  NULL,
    [Bic]           NVARCHAR(20)  NULL,
    [BankName]      NVARCHAR(255) NOT NULL,
    [Country]       NVARCHAR(10)  NOT NULL,
    [LegalAddress]  NVARCHAR(500) NOT NULL,
    [ActualAddress] NVARCHAR(500) NOT NULL,
    [PhoneNumber]   NVARCHAR(20)  NOT NULL,
    [ArchiveLevel]  INT           NOT NULL,
    [CreateTime]    DATETIME2     NOT NULL,
    [ChangeTime]    DATETIME2     NULL,
    [ArchiveTime]   DATETIME2     NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Companies_CompanyCode] UNIQUE ([CompanyCode])
);
CREATE INDEX [IX_Companies_ArchiveLevel] ON [Companies] ([ArchiveLevel]);
CREATE INDEX [IX_Companies_Inn]          ON [Companies] ([Inn]);

CREATE TABLE [Rights]
(
    [Id]           INT           IDENTITY(1,1) NOT NULL,
    [Code]         NVARCHAR(100) NOT NULL,
    [Name]         NVARCHAR(255) NOT NULL,
    [Module]       NVARCHAR(100) NULL,
    [Description]  NVARCHAR(500) NULL,
    [ArchiveLevel] INT           NOT NULL,
    [CreateTime]   DATETIME2     NOT NULL,
    [ChangeTime]   DATETIME2     NULL,
    [ArchiveTime]  DATETIME2     NULL,
    CONSTRAINT [PK_Rights] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Rights_Code] UNIQUE ([Code])
);

CREATE TABLE [Users]
(
    [Id]           INT           IDENTITY(1,1) NOT NULL,
    [Name]         NVARCHAR(100) NOT NULL,
    [Surname]      NVARCHAR(100) NOT NULL,
    [MiddleName]   NVARCHAR(100) NULL,
    [Email]        NVARCHAR(255) NOT NULL,
    [PhoneNumber]  NVARCHAR(20)  NOT NULL,
    [Position]     NVARCHAR(255) NOT NULL,
    [ArchiveLevel] INT           NOT NULL,
    [CreateTime]   DATETIME2     NOT NULL,
    [ChangeTime]   DATETIME2     NULL,
    [ArchiveTime]  DATETIME2     NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
);

CREATE TABLE [CatalogProducts]
(
    [Id]                INT            IDENTITY(1,1) NOT NULL,
    [Isrc]              NVARCHAR(15)   NOT NULL,
    [Barcode]           NVARCHAR(20)   NOT NULL,
    [CatalogNumber]     NVARCHAR(100)  NOT NULL,
    [ProductFormatCode] NVARCHAR(50)   NOT NULL,
    [ProductName]       NVARCHAR(255)  NULL,
    [AlbumName]         NVARCHAR(255)  NULL,
    [Artist]            NVARCHAR(255)  NULL,
    [Composer]          NVARCHAR(255)  NULL,
    [Genre]             NVARCHAR(100)  NULL,
    [ReleaseDate]       DATE           NULL,
    [Description]       NVARCHAR(1000) NULL,
    [ProductTypeDesc]   NVARCHAR(500)  NULL,
    [ProductTypeId]     INT            NOT NULL,
    [ArchiveLevel]      INT            NOT NULL,
    [CreateTime]        DATETIME2      NOT NULL,
    [ChangeTime]        DATETIME2      NULL,
    [ArchiveTime]       DATETIME2      NULL,
    CONSTRAINT [PK_CatalogProducts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CatalogProducts_CatalogProductTypes] FOREIGN KEY ([ProductTypeId])
        REFERENCES [CatalogProductTypes] ([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_CatalogProducts_ArchiveLevel]        ON [CatalogProducts] ([ArchiveLevel]);
CREATE INDEX [IX_CatalogProducts_Isrc]                ON [CatalogProducts] ([Isrc]);
CREATE INDEX [IX_CatalogProducts_Barcode]             ON [CatalogProducts] ([Barcode]);
CREATE INDEX [IX_CatalogProducts_CatalogNumber]       ON [CatalogProducts] ([CatalogNumber]);
CREATE INDEX [IX_CatalogProducts_ProductTypeId]       ON [CatalogProducts] ([ProductTypeId]);
CREATE INDEX [IX_CatalogProducts_ProductName_Artist]  ON [CatalogProducts] ([ProductName], [Artist]);

CREATE TABLE [CatalogProductRights]
(
    [Id]                INT           IDENTITY(1,1) NOT NULL,
    [CatalogProductId]  INT           NOT NULL,
    [CompanySenderId]   INT           NOT NULL,
    [CompanySender]     NVARCHAR(255) NOT NULL,
    [CompanyReceiverId] INT           NOT NULL,
    [CompanyReceiver]   NVARCHAR(255) NOT NULL,
    [TerritoryId]       INT           NOT NULL,
    [TerritoryCode]     NVARCHAR(10)  NOT NULL,
    [TerritoryDesc]     NVARCHAR(255) NOT NULL,
    [DocNumber]         NVARCHAR(100) NULL,
    [DocStart]          DATE          NOT NULL,
    [DocEnd]            DATE          NOT NULL,
    [Share]             FLOAT         NOT NULL,
    [ArchiveLevel]      INT           NOT NULL,
    [CreateTime]        DATETIME2     NOT NULL,
    [ChangeTime]        DATETIME2     NULL,
    [ArchiveTime]       DATETIME2     NULL,
    CONSTRAINT [PK_CatalogProductRights] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CatalogProductRights_CatalogProducts]    FOREIGN KEY ([CatalogProductId])
        REFERENCES [CatalogProducts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CatalogProductRights_CompanySender]      FOREIGN KEY ([CompanySenderId])
        REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CatalogProductRights_CompanyReceiver]    FOREIGN KEY ([CompanyReceiverId])
        REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CatalogProductRights_Territories]        FOREIGN KEY ([TerritoryId])
        REFERENCES [Territories] ([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_CatalogProductRights_ArchiveLevel]      ON [CatalogProductRights] ([ArchiveLevel]);
CREATE INDEX [IX_CatalogProductRights_CatalogProductId]  ON [CatalogProductRights] ([CatalogProductId]);
CREATE INDEX [IX_CatalogProductRights_CompanySenderId]   ON [CatalogProductRights] ([CompanySenderId]);
CREATE INDEX [IX_CatalogProductRights_CompanyReceiverId] ON [CatalogProductRights] ([CompanyReceiverId]);
CREATE INDEX [IX_CatalogProductRights_TerritoryId]       ON [CatalogProductRights] ([TerritoryId]);


CREATE TABLE [Accounts]
(
    [Id]                INT            IDENTITY(1,1) NOT NULL,
    [Login]             NVARCHAR(100)  NOT NULL,
    [PasswordHash]      NVARCHAR(500)  NOT NULL,
    [Description]       NVARCHAR(1000) NULL,
    [UiPreferencesJson] NVARCHAR(MAX)  NULL,
    [UserId]            INT            NULL,
    [ArchiveLevel]      INT            NOT NULL,
    [CreateTime]        DATETIME2      NOT NULL,
    [ChangeTime]        DATETIME2      NULL,
    [ArchiveTime]       DATETIME2      NULL,
    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Accounts_Login] UNIQUE ([Login]),
    CONSTRAINT [FK_Accounts_Users] FOREIGN KEY ([UserId])
        REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
CREATE UNIQUE INDEX [IX_Accounts_UserId] ON [Accounts] ([UserId]) WHERE [UserId] IS NOT NULL;

CREATE TABLE [AccountRightsLinks]
(
    [Id]           INT       IDENTITY(1,1) NOT NULL,
    [AccountId]    INT       NOT NULL,
    [RightId]      INT       NOT NULL,
    [ArchiveLevel] INT       NOT NULL,
    [CreateTime]   DATETIME2 NOT NULL,
    [ChangeTime]   DATETIME2 NULL,
    [ArchiveTime]  DATETIME2 NULL,
    CONSTRAINT [PK_AccountRightsLinks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountRightsLinks_Accounts] FOREIGN KEY ([AccountId])
        REFERENCES [Accounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AccountRightsLinks_Rights]   FOREIGN KEY ([RightId])
        REFERENCES [Rights] ([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_AccountRightsLinks_RightId] ON [AccountRightsLinks] ([RightId]);
CREATE UNIQUE INDEX [IX_AccountRightsLinks_AccountId_RightId]
    ON [AccountRightsLinks] ([AccountId], [RightId]) WHERE [ArchiveLevel] = 0;

CREATE TABLE [RefreshTokens]
(
    [Id]              INT           IDENTITY(1,1) NOT NULL,
    [AccountId]       INT           NOT NULL,
    [Token]           NVARCHAR(256) NOT NULL,
    [ReplacedByToken] NVARCHAR(256) NULL,
    [CreatedAt]       DATETIME2     NOT NULL,
    [ExpiresAt]       DATETIME2     NOT NULL,
    [RevokedAt]       DATETIME2     NULL,
    CONSTRAINT [PK_RefreshTokens]       PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_RefreshTokens_Token] UNIQUE ([Token]),
    CONSTRAINT [FK_RefreshTokens_Accounts] FOREIGN KEY ([AccountId])
        REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_RefreshTokens_AccountId] ON [RefreshTokens] ([AccountId]);

CREATE TABLE [SuspenseGroups]
(
    [Id]               INT           IDENTITY(1,1) NOT NULL,
    [AccountId]        INT           NOT NULL,
    [BusinessStatus]   INT           NOT NULL,
    [CatalogProductId] INT           NULL,
    [MetaDataId]       INT           NULL,
    [MetaRightsId]     INT           NULL,
    [PostponeReason]   NVARCHAR(500) NULL,
    [ArchiveLevel]     INT           NOT NULL,
    [CreateTime]       DATETIME2     NOT NULL,
    [ChangeTime]       DATETIME2     NOT NULL,
    [ArchiveTime]      DATETIME2     NOT NULL,
    CONSTRAINT [PK_SuspenseGroups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SuspenseGroups_Accounts]       FOREIGN KEY ([AccountId])
        REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SuspenseGroups_CatalogProducts] FOREIGN KEY ([CatalogProductId])
        REFERENCES [CatalogProducts] ([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_SuspenseGroups_AccountId]      ON [SuspenseGroups] ([AccountId]);
CREATE INDEX [IX_SuspenseGroups_ArchiveLevel]   ON [SuspenseGroups] ([ArchiveLevel]);
CREATE INDEX [IX_SuspenseGroups_BusinessStatus] ON [SuspenseGroups] ([BusinessStatus]);
CREATE INDEX [IX_SuspenseGroups_CatalogProductId] ON [SuspenseGroups] ([CatalogProductId]);

CREATE TABLE [GroupMetadata]
(
    [Id]              INT            IDENTITY(1,1) NOT NULL,
    [SuspenseGroupId] INT            NOT NULL,
    [CatalogProductId] INT           NULL,
    [ProductTypeId]   INT            NULL,
    [Title]           NVARCHAR(255)  NULL,
    [Artist]          NVARCHAR(255)  NULL,
    [Isrc]            NVARCHAR(15)   NULL,
    [Barcode]         NVARCHAR(20)   NULL,
    [CatalogNumber]   NVARCHAR(100)  NULL,
    [Genre]           NVARCHAR(100)  NULL,
    [ReleaseDate]     DATE           NULL,
    [Duration]        INT            NULL,
    [ProductTypeCode] NVARCHAR(50)   NULL,
    [ProductTypeDesc] NVARCHAR(500)  NULL,
    [Description]     NVARCHAR(1000) NULL,
    [ArchiveLevel]    INT            NOT NULL,
    [CreateTime]      DATETIME2      NOT NULL,
    [ChangeTime]      DATETIME2      NOT NULL,
    [ArchiveTime]     DATETIME2      NOT NULL,
    CONSTRAINT [PK_GroupMetadata] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GroupMetadata_SuspenseGroups]    FOREIGN KEY ([SuspenseGroupId])
        REFERENCES [SuspenseGroups] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_GroupMetadata_CatalogProducts]   FOREIGN KEY ([CatalogProductId])
        REFERENCES [CatalogProducts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GroupMetadata_CatalogProductTypes] FOREIGN KEY ([ProductTypeId])
        REFERENCES [CatalogProductTypes] ([Id]) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX [IX_GroupMetadata_SuspenseGroupId] ON [GroupMetadata] ([SuspenseGroupId]);
CREATE INDEX [IX_GroupMetadata_CatalogProductId]       ON [GroupMetadata] ([CatalogProductId]);
CREATE INDEX [IX_GroupMetadata_ProductTypeId]          ON [GroupMetadata] ([ProductTypeId]);

CREATE TABLE [GroupMetaRights]
(
    [Id]                INT           IDENTITY(1,1) NOT NULL,
    [GroupId]           INT           NOT NULL,
    [CatalogProductId]  INT           NULL,
    [SenderCompanyId]   INT           NULL,
    [ReceiverCompanyId] INT           NULL,
    [TerritoryId]       INT           NULL,
    [TerritoryCode]     NVARCHAR(10)  NULL,
    [TerritoryDesc]     NVARCHAR(255) NULL,
    [DocNumber]         NVARCHAR(100) NULL,
    [DocType]           NVARCHAR(100) NULL,
    [DocDate]           DATE          NULL,
    [DocStart]          DATE          NULL,
    [DocEnd]            DATE          NULL,
    [Share]             FLOAT         NULL,
    [ArchiveLevel]      INT           NOT NULL,
    [CreateTime]        DATETIME2     NOT NULL,
    [ChangeTime]        DATETIME2     NOT NULL,
    [ArchiveTime]       DATETIME2     NOT NULL,
    CONSTRAINT [PK_GroupMetaRights] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GroupMetaRights_SuspenseGroups]  FOREIGN KEY ([GroupId])
        REFERENCES [SuspenseGroups] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_GroupMetaRights_CatalogProducts] FOREIGN KEY ([CatalogProductId])
        REFERENCES [CatalogProducts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GroupMetaRights_SenderCompany]   FOREIGN KEY ([SenderCompanyId])
        REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GroupMetaRights_ReceiverCompany] FOREIGN KEY ([ReceiverCompanyId])
        REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GroupMetaRights_Territories]     FOREIGN KEY ([TerritoryId])
        REFERENCES [Territories] ([Id]) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX [IX_GroupMetaRights_GroupId]          ON [GroupMetaRights] ([GroupId]);
CREATE INDEX [IX_GroupMetaRights_CatalogProductId]        ON [GroupMetaRights] ([CatalogProductId]);
CREATE INDEX [IX_GroupMetaRights_SenderCompanyId]         ON [GroupMetaRights] ([SenderCompanyId]);
CREATE INDEX [IX_GroupMetaRights_ReceiverCompanyId]       ON [GroupMetaRights] ([ReceiverCompanyId]);
CREATE INDEX [IX_GroupMetaRights_TerritoryId]             ON [GroupMetaRights] ([TerritoryId]);

CREATE TABLE [SuspenseLines]
(
    [Id]                 INT           IDENTITY(1,1) NOT NULL,
    [BusinessStatus]     INT           NOT NULL,
    [CauseSuspense]      NVARCHAR(255) NOT NULL,
    [GroupId]            INT           NULL,
    [ProductId]          INT           NULL,
    [SenderCompanyId]    INT           NULL,
    [SenderCompany]      NVARCHAR(255) NULL,
    [RecipientCompanyId] INT           NULL,
    [RecipientCompany]   NVARCHAR(255) NULL,
    [Isrc]               NVARCHAR(15)  NULL,
    [Barcode]            NVARCHAR(20)  NULL,
    [CatalogNumber]      NVARCHAR(100) NULL,
    [Artist]             NVARCHAR(255) NULL,
    [TrackTitle]         NVARCHAR(255) NULL,
    [Genre]              NVARCHAR(100) NULL,
    [AgreementType]      NVARCHAR(100) NULL,
    [AgreementNumber]    NVARCHAR(100) NULL,
    [TerritoryCode]      NVARCHAR(10)  NULL,
    [Operator]           NVARCHAR(255) NULL,
    [Qty]                INT           NOT NULL,
    [Ppd]                FLOAT         NULL,
    [ExchangeCurrency]   NVARCHAR(10)  NULL,
    [ExchangeRate]       DECIMAL(18,6) NOT NULL,
    [ArchiveLevel]       INT           NOT NULL,
    [CreateTime]         DATETIME2     NOT NULL,
    [ChangeTime]         DATETIME2     NULL,
    [ArchiveTime]        DATETIME2     NULL,
    CONSTRAINT [PK_SuspenseLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SuspenseLines_SuspenseGroups]     FOREIGN KEY ([GroupId])
        REFERENCES [SuspenseGroups] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_SuspenseLines_CatalogProducts]    FOREIGN KEY ([ProductId])
        REFERENCES [CatalogProducts] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_SuspenseLines_SenderCompany]      FOREIGN KEY ([SenderCompanyId])
        REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SuspenseLines_RecipientCompany]   FOREIGN KEY ([RecipientCompanyId])
        REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_SuspenseLines_ArchiveLevel]        ON [SuspenseLines] ([ArchiveLevel]);
CREATE INDEX [IX_SuspenseLines_BusinessStatus]      ON [SuspenseLines] ([BusinessStatus]);
CREATE INDEX [IX_SuspenseLines_GroupId]             ON [SuspenseLines] ([GroupId]);
CREATE INDEX [IX_SuspenseLines_ProductId]           ON [SuspenseLines] ([ProductId]);
CREATE INDEX [IX_SuspenseLines_Isrc]                ON [SuspenseLines] ([Isrc]);
CREATE INDEX [IX_SuspenseLines_Barcode]             ON [SuspenseLines] ([Barcode]);
CREATE INDEX [IX_SuspenseLines_SenderCompanyId]     ON [SuspenseLines] ([SenderCompanyId]);
CREATE INDEX [IX_SuspenseLines_RecipientCompanyId]  ON [SuspenseLines] ([RecipientCompanyId]);

CREATE TABLE [SuspenseGroupLinks]
(
    [Id]              INT       IDENTITY(1,1) NOT NULL,
    [SuspenseGroupId] INT       NOT NULL,
    [SuspenseId]      INT       NOT NULL,
    [AccountId]       INT       NOT NULL,
    [BusinessStatus]  INT       NOT NULL,
    [ArchiveLevel]    INT       NOT NULL,
    [CreateTime]      DATETIME2 NOT NULL,
    [ChangeTime]      DATETIME2 NOT NULL,
    [ArchiveTime]     DATETIME2 NOT NULL,
    CONSTRAINT [PK_SuspenseGroupLinks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SuspenseGroupLinks_SuspenseGroups] FOREIGN KEY ([SuspenseGroupId])
        REFERENCES [SuspenseGroups] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SuspenseGroupLinks_SuspenseLines]  FOREIGN KEY ([SuspenseId])
        REFERENCES [SuspenseLines] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SuspenseGroupLinks_Accounts]       FOREIGN KEY ([AccountId])
        REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_SuspenseGroupLinks_AccountId]            ON [SuspenseGroupLinks] ([AccountId]);
CREATE INDEX [IX_SuspenseGroupLinks_SuspenseGroupId]      ON [SuspenseGroupLinks] ([SuspenseGroupId]);
CREATE UNIQUE INDEX [IX_SuspenseGroupLinks_SuspenseId_SuspenseGroupId]
    ON [SuspenseGroupLinks] ([SuspenseId], [SuspenseGroupId]);

CREATE TABLE [BackOfficeTasks]
(
    [Id]                 INT            IDENTITY(1,1) NOT NULL,
    [GroupId]            INT            NOT NULL,
    [CreatedByAccountId] INT            NOT NULL,
    [TaskStatus]         INT            NOT NULL,
    [ProblemDescription] NVARCHAR(2000) NOT NULL,
    [ArchiveLevel]       INT            NOT NULL CONSTRAINT [DF_BackOfficeTasks_ArchiveLevel] DEFAULT 0,
    [CreateTime]         DATETIME2      NOT NULL,
    [ChangeTime]         DATETIME2      NULL,
    [ArchiveTime]        DATETIME2      NULL,
    CONSTRAINT [PK_BackOfficeTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BackOfficeTasks_SuspenseGroups] FOREIGN KEY ([GroupId])
        REFERENCES [SuspenseGroups] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BackOfficeTasks_Accounts]       FOREIGN KEY ([CreatedByAccountId])
        REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_BackOfficeTasks_ArchiveLevel]        ON [BackOfficeTasks] ([ArchiveLevel]);
CREATE INDEX [IX_BackOfficeTasks_GroupId]             ON [BackOfficeTasks] ([GroupId]);
CREATE INDEX [IX_BackOfficeTasks_CreatedByAccountId]  ON [BackOfficeTasks] ([CreatedByAccountId]);
CREATE INDEX [IX_BackOfficeTasks_TaskStatus]          ON [BackOfficeTasks] ([TaskStatus]);

CREATE TABLE [SuspenseGroupLogs]
(
    [Id]              INT           IDENTITY(1,1) NOT NULL,
    [SuspenseGroupId] INT           NOT NULL,
    [AccountId]       INT           NOT NULL,
    [AccountLogin]    NVARCHAR(200) NOT NULL,
    [AccountName]     NVARCHAR(300) NULL,
    [StatusFrom]      INT           NULL,
    [StatusTo]        INT           NOT NULL,
    [OperationTime]   DATETIME2     NOT NULL,
    CONSTRAINT [PK_SuspenseGroupLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SuspenseGroupLogs_SuspenseGroups] FOREIGN KEY ([SuspenseGroupId])
        REFERENCES [SuspenseGroups] ([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_SuspenseGroupLogs_AccountId]      ON [SuspenseGroupLogs] ([AccountId]);
CREATE INDEX [IX_SuspenseGroupLogs_SuspenseGroupId] ON [SuspenseGroupLogs] ([SuspenseGroupId]);
CREATE INDEX [IX_SuspenseGroupLogs_OperationTime]  ON [SuspenseGroupLogs] ([OperationTime]);

CREATE TABLE [SuspenseLineLogs]
(
    [Id]             INT           IDENTITY(1,1) NOT NULL,
    [SuspenseLineId] INT           NOT NULL,
    [AccountId]      INT           NOT NULL,
    [AccountLogin]   NVARCHAR(200) NOT NULL,
    [AccountName]    NVARCHAR(300) NULL,
    [GroupId]        INT           NULL,
    [StatusFrom]     INT           NULL,
    [StatusTo]       INT           NOT NULL,
    [OperationTime]  DATETIME2     NOT NULL,
    CONSTRAINT [PK_SuspenseLineLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SuspenseLineLogs_SuspenseLines] FOREIGN KEY ([SuspenseLineId])
        REFERENCES [SuspenseLines] ([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_SuspenseLineLogs_AccountId]     ON [SuspenseLineLogs] ([AccountId]);
CREATE INDEX [IX_SuspenseLineLogs_SuspenseLineId] ON [SuspenseLineLogs] ([SuspenseLineId]);
CREATE INDEX [IX_SuspenseLineLogs_GroupId]        ON [SuspenseLineLogs] ([GroupId]);
CREATE INDEX [IX_SuspenseLineLogs_OperationTime]  ON [SuspenseLineLogs] ([OperationTime]);
