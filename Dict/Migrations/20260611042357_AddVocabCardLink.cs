using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabCardLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardId",
                table: "project_vocabularies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_vocabularies_CardId",
                table: "project_vocabularies",
                column: "CardId");

            migrationBuilder.AddForeignKey(
                name: "FK_project_vocabularies_cards_CardId",
                table: "project_vocabularies",
                column: "CardId",
                principalTable: "cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_vocabularies_cards_CardId",
                table: "project_vocabularies");

            migrationBuilder.DropIndex(
                name: "IX_project_vocabularies_CardId",
                table: "project_vocabularies");

            migrationBuilder.DropColumn(
                name: "CardId",
                table: "project_vocabularies");
        }
    }
}
