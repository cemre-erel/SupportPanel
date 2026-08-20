using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportPanel.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleProductManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProducts_ProductId",
                table: "UserProducts");

            migrationBuilder.CreateIndex(
                name: "IX_UserProducts_ProductId",
                table: "UserProducts",
                column: "ProductId",
                unique: true,
                filter: "[IsActive] = 1 AND [IsProductManager] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProducts_ProductId",
                table: "UserProducts");

            migrationBuilder.CreateIndex(
                name: "IX_UserProducts_ProductId",
                table: "UserProducts",
                column: "ProductId");
        }
    }
}
