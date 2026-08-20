using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportPanel.Migrations
{
    /// <inheritdoc />
    public partial class DetachInternalUsersFromTenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [u]
                SET [u].[TenantId] = NULL
                FROM [Users] AS [u]
                INNER JOIN [Roles] AS [r] ON [u].[RoleId] = [r].[Id]
                WHERE [r].[Name] IN ('SystemAdmin', 'ProductManager', 'SupportSpecialist');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eski firma bağlantıları güvenilir biçimde belirlenemeyeceği için geri yüklenmez.
        }
    }
}
