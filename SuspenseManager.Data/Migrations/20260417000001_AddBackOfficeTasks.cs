using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBackOfficeTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackOfficeTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: false),
                    ProblemDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TaskStatus = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ArchiveTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackOfficeTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackOfficeTasks_Accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BackOfficeTasks_SuspenseGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "SuspenseGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeTasks_ArchiveLevel",
                table: "BackOfficeTasks",
                column: "ArchiveLevel");

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeTasks_CreatedByAccountId",
                table: "BackOfficeTasks",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeTasks_GroupId",
                table: "BackOfficeTasks",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeTasks_TaskStatus",
                table: "BackOfficeTasks",
                column: "TaskStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackOfficeTasks");
        }
    }
}
