using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntryUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_entries_EntSeq",
                table: "entries");

            migrationBuilder.CreateIndex(
                name: "IX_entries_EntSeq_Type",
                table: "entries",
                columns: new[] { "EntSeq", "Type" },
                unique: true,
                filter: "[EntSeq] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_entries_EntSeq_Type",
                table: "entries");

            migrationBuilder.CreateIndex(
                name: "IX_entries_EntSeq",
                table: "entries",
                column: "EntSeq",
                unique: true,
                filter: "[EntSeq] IS NOT NULL");
        }
    }
}
