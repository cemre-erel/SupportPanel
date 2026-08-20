using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportPanel.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProductResponsibilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProductManager",
                table: "UserProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSupportSpecialist",
                table: "UserProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE up
                SET IsProductManager = CASE WHEN r.Name = 'ProductManager' THEN 1 ELSE 0 END,
                    IsSupportSpecialist = CASE WHEN r.Name = 'SupportSpecialist' THEN 1 ELSE 0 END
                FROM UserProducts up
                INNER JOIN Users u ON u.Id = up.UserId
                INNER JOIN Roles r ON r.Id = u.RoleId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProductManager",
                table: "UserProducts");

            migrationBuilder.DropColumn(
                name: "IsSupportSpecialist",
                table: "UserProducts");
        }
    }
}
