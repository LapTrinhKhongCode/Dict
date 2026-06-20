using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    public partial class AddDocumentOverviewToOcrJob : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentOverview",
                table: "ocr_jobs",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentOverview",
                table: "ocr_jobs");
        }
    }
}
