using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabSourceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceOcrJobId",
                table: "project_vocabularies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourcePage",
                table: "project_vocabularies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSentence",
                table: "project_vocabularies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_vocabularies_SourceOcrJobId",
                table: "project_vocabularies",
                column: "SourceOcrJobId");

            migrationBuilder.AddForeignKey(
                name: "FK_project_vocabularies_ocr_jobs_SourceOcrJobId",
                table: "project_vocabularies",
                column: "SourceOcrJobId",
                principalTable: "ocr_jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_vocabularies_ocr_jobs_SourceOcrJobId",
                table: "project_vocabularies");

            migrationBuilder.DropIndex(
                name: "IX_project_vocabularies_SourceOcrJobId",
                table: "project_vocabularies");

            migrationBuilder.DropColumn(
                name: "SourceOcrJobId",
                table: "project_vocabularies");

            migrationBuilder.DropColumn(
                name: "SourcePage",
                table: "project_vocabularies");

            migrationBuilder.DropColumn(
                name: "SourceSentence",
                table: "project_vocabularies");
        }
    }
}
