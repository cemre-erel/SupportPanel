using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportPanel.Data;

#nullable disable

namespace SupportPanel.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260817130000_BackfillFirstResponseDates")]
    public partial class BackfillFirstResponseDates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE t
                SET t.[FirstResponseDate] = response.[FirstResponseDate]
                FROM [Tickets] t
                CROSS APPLY
                (
                    SELECT MIN(h.[ActionDate]) AS [FirstResponseDate]
                    FROM [TicketHistories] h
                    WHERE h.[TicketId] = t.[Id]
                      AND h.[Action] = N'Durum değiştirildi'
                      AND h.[NewValue] IN ('InReview', 'WaitingCustomer', 'Resolved')
                ) response
                WHERE t.[FirstResponseDate] IS NULL
                  AND response.[FirstResponseDate] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geçmişten türetilen doğru zaman bilgisi geri alınmaz.
        }
    }
}
