using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAcceptedRequestDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_CraftsmanId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_CustomerId_CraftsmanId_CraftId_NeededOn",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CraftsmanId_NeededOn",
                table: "Requests",
                columns: new[] { "CraftsmanId", "NeededOn" },
                unique: true,
                filter: "Status = 'Accepted'");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CustomerId",
                table: "Requests",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_CraftsmanId_NeededOn",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_CustomerId",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CraftsmanId",
                table: "Requests",
                column: "CraftsmanId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CustomerId_CraftsmanId_CraftId_NeededOn",
                table: "Requests",
                columns: new[] { "CustomerId", "CraftsmanId", "CraftId", "NeededOn" },
                unique: true,
                filter: "Status IN ('Pending', 'Accepted')");
        }
    }
}
