using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportPanel.Data;

#nullable disable

namespace SupportPanel.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260817140000_RecalculateFirstResponseFromSupportComments")]
    public partial class RecalculateFirstResponseFromSupportComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE t
                SET FirstResponseDate = support.FirstResponseDate
                FROM Tickets AS t
                OUTER APPLY
                (
                    SELECT MIN(c.CreatedDate) AS FirstResponseDate
                    FROM TicketComments AS c
                    INNER JOIN Users AS u ON u.Id = c.UserId
                    INNER JOIN Roles AS r ON r.Id = u.RoleId
                    WHERE c.TicketId = t.Id
                      AND r.Name IN ('SupportSpecialist', 'ProductManager')
                ) AS support;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Önceki değerlerin hangi durum değişikliğinden üretildiği güvenilir biçimde
            // belirlenemediği için bu veri düzeltmesi geri alınamaz.
        }
    }
}
