using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportPanel.Data;

#nullable disable

namespace SupportPanel.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260817131000_ClearSlaForInternallyClosedTickets")]
    public partial class ClearSlaForInternallyClosedTickets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE t
                SET t.[SlaStartedDate] = NULL
                FROM [Tickets] t
                WHERE t.[Status] = 'Closed'
                  AND t.[FirstResponseDate] IS NULL
                  AND EXISTS
                  (
                      SELECT 1
                      FROM [TicketHistories] closeHistory
                      WHERE closeHistory.[TicketId] = t.[Id]
                        AND closeHistory.[Action] = N'Durum değiştirildi'
                        AND closeHistory.[OldValue] = 'New'
                        AND closeHistory.[NewValue] = 'Closed'
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [TicketHistories] supportHistory
                      WHERE supportHistory.[TicketId] = t.[Id]
                        AND supportHistory.[Action] = N'Durum değiştirildi'
                        AND supportHistory.[NewValue] IN
                            ('SupportQueue', 'Assigned', 'InReview', 'WaitingCustomer', 'Resolved')
                  )");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Desteğe hiç ulaşmamış talepler için yapay SLA başlangıcı geri yüklenmez.
        }
    }
}
