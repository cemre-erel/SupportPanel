using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportPanel.Migrations
{
    /// <inheritdoc />
    public partial class UseTenantSlaCalculationMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalculationMethod",
                table: "SlaLevels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalculationMethod",
                table: "SlaLevels",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
