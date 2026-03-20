using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneticAndRomajiToEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<string>(
            //    name: "CommentRawJson",
            //    table: "entries",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<string>(
            //    name: "JsonErrorMessage",
            //    table: "entries",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<string>(
            //    name: "JsonProcessingStatus",
            //    table: "entries",
            //    type: "nvarchar(50)",
            //    maxLength: 50,
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<int>(
            //    name: "MobileId",
            //    table: "entries",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<string>(
            //    name: "Phonetic",
            //    table: "entries",
            //    type: "nvarchar(255)",
            //    maxLength: 255,
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<string>(
            //    name: "Romaji",
            //    table: "entries",
            //    type: "nvarchar(255)",
            //    maxLength: 255,
            //    nullable: false,
            //    defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentRawJson",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "JsonErrorMessage",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "JsonProcessingStatus",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "MobileId",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "Phonetic",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "Romaji",
                table: "entries");
        }
    }
}
