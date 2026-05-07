using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class FilterAccountRightsLinksUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountRightsLinks_AccountId_RightId",
                table: "AccountRightsLinks");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRightsLinks_AccountId_RightId",
                table: "AccountRightsLinks",
                columns: new[] { "AccountId", "RightId" },
                unique: true,
                filter: "[ArchiveLevel] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountRightsLinks_AccountId_RightId",
                table: "AccountRightsLinks");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRightsLinks_AccountId_RightId",
                table: "AccountRightsLinks",
                columns: new[] { "AccountId", "RightId" },
                unique: true);
        }
    }
}
