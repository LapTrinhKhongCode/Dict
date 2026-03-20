using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class CreateSearchIndexesForEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateIndex(
            //    name: "IX_entries_Type_Label",
            //    table: "entries",
            //    columns: new[] { "Type", "Label" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_entries_Type_Phonetic",
            //    table: "entries",
            //    columns: new[] { "Type", "Phonetic" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_entries_Type_Label",
                table: "entries");

            migrationBuilder.DropIndex(
                name: "IX_entries_Type_Phonetic",
                table: "entries");
        }
    }
}
