using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SupportPanel.Data;

#nullable disable

namespace SupportPanel.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260814160000_StartSlaWhenRoutedToSupport")]
    public partial class StartSlaWhenRoutedToSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SlaStartedDate",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            // Mevcut taleplerin SLA sonuçları değişmesin; yeni kural yalnızca
            // bundan sonra firma kullanıcılarının açacağı taleplerde uygulanır.
            migrationBuilder.Sql(
                "UPDATE [Tickets] SET [SlaStartedDate] = [CreatedDate]");

            // Henüz desteğe ulaşmamış firma kullanıcısı talepleri yeni kuralla
            // sayaç başlamadan beklemelidir.
            migrationBuilder.Sql(@"
                UPDATE t
                SET t.[SlaStartedDate] = NULL
                FROM [Tickets] t
                INNER JOIN [Users] u ON u.[Id] = t.[CreatedByUserId]
                INNER JOIN [Roles] r ON r.[Id] = u.[RoleId]
                WHERE t.[Status] = 'New' AND r.[Name] = 'CompanyUser'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlaStartedDate",
                table: "Tickets");
        }
    }
}
