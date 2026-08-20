using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportPanel.Data;

#nullable disable

namespace SupportPanel.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260813083526_ConvertSlaTargetsToMinutes")]
    public partial class ConvertSlaTargetsToMinutes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResponseTargetHours",
                table: "SlaLevels",
                newName: "ResponseTargetMinutes");

            migrationBuilder.RenameColumn(
                name: "ResolutionTargetHours",
                table: "SlaLevels",
                newName: "ResolutionTargetMinutes");

            migrationBuilder.Sql("UPDATE SlaLevels SET ResponseTargetMinutes = ResponseTargetMinutes * 60, ResolutionTargetMinutes = ResolutionTargetMinutes * 60");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE SlaLevels SET ResponseTargetMinutes = CASE WHEN ResponseTargetMinutes < 60 THEN 1 ELSE ResponseTargetMinutes / 60 END, ResolutionTargetMinutes = CASE WHEN ResolutionTargetMinutes < 60 THEN 1 ELSE ResolutionTargetMinutes / 60 END");

            migrationBuilder.RenameColumn(
                name: "ResponseTargetMinutes",
                table: "SlaLevels",
                newName: "ResponseTargetHours");

            migrationBuilder.RenameColumn(
                name: "ResolutionTargetMinutes",
                table: "SlaLevels",
                newName: "ResolutionTargetHours");
        }
    }
}
