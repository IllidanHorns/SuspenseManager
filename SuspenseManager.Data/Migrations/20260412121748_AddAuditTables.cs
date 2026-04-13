using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatusDictionary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusDictionary", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuspenseGroupLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuspenseGroupId = table.Column<int>(type: "int", nullable: false),
                    StatusFrom = table.Column<int>(type: "int", nullable: true),
                    StatusTo = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    AccountLogin = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OperationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspenseGroupLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuspenseGroupLogs_SuspenseGroups_SuspenseGroupId",
                        column: x => x.SuspenseGroupId,
                        principalTable: "SuspenseGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SuspenseLineLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuspenseLineId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    StatusFrom = table.Column<int>(type: "int", nullable: true),
                    StatusTo = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    AccountLogin = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OperationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspenseLineLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuspenseLineLogs_SuspenseLines_SuspenseLineId",
                        column: x => x.SuspenseLineId,
                        principalTable: "SuspenseLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatusDictionary_Code",
                table: "StatusDictionary",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseGroupLogs_AccountId",
                table: "SuspenseGroupLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseGroupLogs_OperationTime",
                table: "SuspenseGroupLogs",
                column: "OperationTime");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseGroupLogs_SuspenseGroupId",
                table: "SuspenseGroupLogs",
                column: "SuspenseGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLineLogs_AccountId",
                table: "SuspenseLineLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLineLogs_GroupId",
                table: "SuspenseLineLogs",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLineLogs_OperationTime",
                table: "SuspenseLineLogs",
                column: "OperationTime");

            migrationBuilder.CreateIndex(
                name: "IX_SuspenseLineLogs_SuspenseLineId",
                table: "SuspenseLineLogs",
                column: "SuspenseLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatusDictionary");

            migrationBuilder.DropTable(
                name: "SuspenseGroupLogs");

            migrationBuilder.DropTable(
                name: "SuspenseLineLogs");
        }
    }
}
