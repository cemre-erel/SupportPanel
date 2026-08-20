using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportPanel.Migrations
{
    /// <inheritdoc />
    public partial class SlaLevelsAndTicketFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Tickets_TicketId",
                table: "TicketHistories");

            migrationBuilder.CreateTable(
                name: "SlaLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlaLevels_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("INSERT INTO SlaLevels (TenantId, Name, IsActive) SELECT Id, 'Standart', 1 FROM Tenants");
            migrationBuilder.Sql("INSERT INTO SlaLevels (TenantId, Name, IsActive) SELECT Id, 'Yüksek', 1 FROM Tenants");
            migrationBuilder.Sql("INSERT INTO SlaLevels (TenantId, Name, IsActive) SELECT Id, 'Kritik', 1 FROM Tenants");

            migrationBuilder.AlterColumn<int>(
                name: "AssignedUserId",
                table: "Tickets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "SlaLevelId",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE T SET SlaLevelId = ISNULL((SELECT TOP 1 Id FROM SlaLevels S WHERE S.TenantId = T.TenantId AND S.Name = T.SLALevel), (SELECT TOP 1 Id FROM SlaLevels S WHERE S.TenantId = T.TenantId ORDER BY S.Id)) FROM Tickets T");

            migrationBuilder.DropColumn(
                name: "SLALevel",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SlaLevelId",
                table: "Tickets",
                column: "SlaLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaLevels_TenantId",
                table: "SlaLevels",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Tickets_TicketId",
                table: "TicketHistories",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_SlaLevels_SlaLevelId",
                table: "Tickets",
                column: "SlaLevelId",
                principalTable: "SlaLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Tickets_TicketId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_SlaLevels_SlaLevelId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SlaLevelId",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "SLALevel",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE T SET SLALevel = ISNULL((SELECT TOP 1 Name FROM SlaLevels S WHERE S.Id = T.SlaLevelId), 'Standart') FROM Tickets T");

            migrationBuilder.DropColumn(
                name: "SlaLevelId",
                table: "Tickets");

            migrationBuilder.AlterColumn<int>(
                name: "AssignedUserId",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropTable(
                name: "SlaLevels");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Tickets_TicketId",
                table: "TicketHistories",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
