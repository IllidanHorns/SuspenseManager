using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogProductTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogProductTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LegalAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActualAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Inn = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Bic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Territories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TerritoryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TerritoryName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Territories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductFormatCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductTypeDesc = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProductTypeId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AlbumName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Artist = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CatalogNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Composer = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Isrc = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Genre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogProducts_CatalogProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "CatalogProductTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Login = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CatalogProductRights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanySender = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CompanyReceiver = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CompanySenderId = table.Column<int>(type: "int", nullable: false),
                    CompanyReceiverId = table.Column<int>(type: "int", nullable: false),
                    Share = table.Column<double>(type: "float", nullable: false),
                    TerritoryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TerritoryDesc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TerritoryId = table.Column<int>(type: "int", nullable: false),
                    CatalogProductId = table.Column<int>(type: "int", nullable: false),
                    DocStart = table.Column<DateOnly>(type: "date", nullable: false),
                    DocEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogProductRights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogProductRights_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogProductRights_Companies_CompanyReceiverId",
                        column: x => x.CompanyReceiverId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogProductRights_Companies_CompanySenderId",
                        column: x => x.CompanySenderId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogProductRights_Territories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "Territories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountRightsLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RightId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRightsLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountRightsLinks_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountRightsLinks_Rights_RightId",
                        column: x => x.RightId,
                        principalTable: "Rights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuspenseGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessStatus = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CatalogProductId = table.Column<int>(type: "int", nullable: true),
                    MetaDataId = table.Column<int>(type: "int", nullable: true),
                    MetaRightsId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspenseGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuspenseGroups_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuspenseGroups_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuspenseGroupId = table.Column<int>(type: "int", nullable: false),
                    CatalogNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Isrc = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Artist = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Genre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProductTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductTypeDesc = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProductTypeId = table.Column<int>(type: "int", nullable: true),
                    CatalogProductId = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMetadata_CatalogProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "CatalogProductTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMetadata_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMetadata_SuspenseGroups_SuspenseGroupId",
                        column: x => x.SuspenseGroupId,
                        principalTable: "SuspenseGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMetaRights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    DocNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DocStart = table.Column<DateOnly>(type: "date", nullable: true),
                    DocEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    TerritoryId = table.Column<int>(type: "int", nullable: true),
                    TerritoryDesc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TerritoryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CatalogProductId = table.Column<int>(type: "int", nullable: true),
                    SenderCompanyId = table.Column<int>(type: "int", nullable: true),
                    ReceiverCompanyId = table.Column<int>(type: "int", nullable: true),
                    Share = table.Column<double>(type: "float", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMetaRights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMetaRights_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMetaRights_Companies_ReceiverCompanyId",
                        column: x => x.ReceiverCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMetaRights_Companies_SenderCompanyId",
                        column: x => x.SenderCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMetaRights_SuspenseGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "SuspenseGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMetaRights_Territories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "Territories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SuspenseLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Isrc = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CatalogNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    SenderCompany = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RecipientCompany = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Operator = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Artist = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TrackTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AgreementType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AgreementNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TerritoryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CauseSuspense = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    Ppd = table.Column<double>(type: "float", nullable: true),
                    ExchangeCurrency = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BusinessStatus = table.Column<int>(type: "int", nullable: false),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Genre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    SenderCompanyId = table.Column<int>(type: "int", nullable: true),
                    RecipientCompanyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspenseLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuspenseLines_CatalogProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SuspenseLines_Companies_RecipientCompanyId",
                        column: x => x.RecipientCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuspenseLines_Companies_SenderCompanyId",
                        column: x => x.SenderCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuspenseLines_SuspenseGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "SuspenseGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SuspenseGroupLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuspenseId = table.Column<int>(type: "int", nullable: false),
                    SuspenseGroupId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false),
                    BusinessStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspenseGroupLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuspenseGroupLinks_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuspenseGroupLinks_SuspenseGroups_SuspenseGroupId",
                        column: x => x.SuspenseGroupId,
                        principalTable: "SuspenseGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuspenseGroupLinks_SuspenseLines_SuspenseId",
                        column: x => x.SuspenseId,
                        principalTable: "SuspenseLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRightsLinks_AccountId_RightId",
                table: "AccountRightsLinks",
                columns: new[] { "AccountId", "RightId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRightsLinks_RightId",
                table: "AccountRightsLinks",
                column: "RightId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Login",
                table: "Accounts",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProductRights_ArchiveLevel",
                table: "CatalogProductRights",
                column: "ArchiveLevel");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProductRights_CatalogProductId",
                table: "CatalogProductRights",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProductRights_CompanyReceiverId",
                table: "CatalogProductRights",
                column: "CompanyReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProductRights_CompanySenderId",
                table: "CatalogProductRights",
                column: "CompanySenderId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProductRights_TerritoryId",
                table: "CatalogProductRights",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_ArchiveLevel",
                table: "CatalogProducts",
                column: "ArchiveLevel");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_Barcode",
                table: "CatalogProducts",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_CatalogNumber",
                table: "CatalogProducts",
                column: "CatalogNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_Isrc",
                table: "CatalogProducts",
                column: "Isrc");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_ProductName_Artist",
                table: "CatalogProducts",
                columns: new[] { "ProductName", "Artist" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_ProductTypeId",
                table: "CatalogProducts",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ArchiveLevel",
                table: "Companies",
                column: "ArchiveLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyCode",
                table: "Companies",
                column: "CompanyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Inn",
                table: "Companies",
                column: "Inn");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMetadata_CatalogProductId",
                table: "GroupMetadata",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMetadata_ProductTypeId",
                table: "GroupMetadata",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMetadata_SuspenseGroupId",
                table: "GroupMetadata",
                column: "SuspenseGroupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMetaRights_CatalogProductId",
                table: "GroupMetaRights",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMetaRights_GroupId",
                table: "GroupMetaRights",
                column: "GroupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMetaRights_ReceiverCompanyId",
                table: "GroupMetaRights",
                column: "ReceiverCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMetaRights_SenderCompanyId",
                table: "GroupMetaRights",
                column: "SenderCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMetaRights_TerritoryId",
                table: "GroupMetaRights",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Rights_Code",
                table: "Rights",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseGroupLinks_AccountId",
                table: "SuspenseGroupLinks",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseGroupLinks_SuspenseGroupId",
                table: "SuspenseGroupLinks",
                column: "SuspenseGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseGroupLinks_SuspenseId_SuspenseGroupId",
                table: "SuspenseGroupLinks",
                columns: new[] { "SuspenseId", "SuspenseGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseGroups_AccountId",
                table: "SuspenseGroups",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseGroups_ArchiveLevel",
                table: "SuspenseGroups",
                column: "ArchiveLevel");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseGroups_BusinessStatus",
                table: "SuspenseGroups",
                column: "BusinessStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseGroups_CatalogProductId",
                table: "SuspenseGroups",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLines_ArchiveLevel",
                table: "SuspenseLines",
                column: "ArchiveLevel");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLines_Barcode",
                table: "SuspenseLines",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLines_BusinessStatus",
                table: "SuspenseLines",
                column: "BusinessStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLines_GroupId",
                table: "SuspenseLines",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLines_Isrc",
                table: "SuspenseLines",
                column: "Isrc");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLines_ProductId",
                table: "SuspenseLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLines_RecipientCompanyId",
                table: "SuspenseLines",
                column: "RecipientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLines_SenderCompanyId",
                table: "SuspenseLines",
                column: "SenderCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Territories_TerritoryCode",
                table: "Territories",
                column: "TerritoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountRightsLinks");

            migrationBuilder.DropTable(
                name: "CatalogProductRights");

            migrationBuilder.DropTable(
                name: "GroupMetadata");

            migrationBuilder.DropTable(
                name: "GroupMetaRights");

            migrationBuilder.DropTable(
                name: "SuspenseGroupLinks");

            migrationBuilder.DropTable(
                name: "Rights");

            migrationBuilder.DropTable(
                name: "Territories");

            migrationBuilder.DropTable(
                name: "SuspenseLines");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "SuspenseGroups");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "CatalogProducts");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "CatalogProductTypes");
        }
    }
}
