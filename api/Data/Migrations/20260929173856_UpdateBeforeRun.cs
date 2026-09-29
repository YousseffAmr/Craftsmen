using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBeforeRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_CraftsmanId_NeededOn",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CraftsmanId_NeededOn",
                table: "Requests",
                columns: new[] { "CraftsmanId", "NeededOn" },
                unique: true,
                filter: "\"Status\" = 'Accepted'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_CraftsmanId_NeededOn",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CraftsmanId_NeededOn",
                table: "Requests",
                columns: new[] { "CraftsmanId", "NeededOn" },
                unique: true,
                filter: "Status = 'Accepted'");
        }
    }
}
