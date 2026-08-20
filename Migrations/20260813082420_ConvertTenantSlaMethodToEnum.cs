using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportPanel.Migrations
{
    /// <inheritdoc />
    public partial class ConvertTenantSlaMethodToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SLACalculationMethodValue",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE [Tenants]
                SET [SLACalculationMethodValue] = CASE
                    WHEN LOWER(LTRIM(RTRIM([SLACalculationMethod]))) IN
                        (N'7x24', N'7/24', N'aroundtheclock', N'gerçek geçen süre') THEN 0
                    ELSE 1
                END;
                """);

            migrationBuilder.DropColumn(
                name: "SLACalculationMethod",
                table: "Tenants");

            migrationBuilder.RenameColumn(
                name: "SLACalculationMethodValue",
                table: "Tenants",
                newName: "SLACalculationMethod");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SLACalculationMethodText",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Çalışma Saatleri");

            migrationBuilder.Sql("""
                UPDATE [Tenants]
                SET [SLACalculationMethodText] = CASE
                    WHEN [SLACalculationMethod] = 0 THEN N'7x24'
                    ELSE N'Çalışma Saatleri'
                END;
                """);

            migrationBuilder.DropColumn(
                name: "SLACalculationMethod",
                table: "Tenants");

            migrationBuilder.RenameColumn(
                name: "SLACalculationMethodText",
                table: "Tenants",
                newName: "SLACalculationMethod");
        }
    }
}
