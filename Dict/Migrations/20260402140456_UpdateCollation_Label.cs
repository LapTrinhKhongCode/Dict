using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCollation_Label : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Phonetic",
                table: "entries",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "Japanese_CS_AS_KS_WS",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "entries",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                collation: "Japanese_CS_AS_KS_WS",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Phonetic",
                table: "entries",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldCollation: "Japanese_CS_AS_KS_WS");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "entries",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldCollation: "Japanese_CS_AS_KS_WS");
        }
    }
}
