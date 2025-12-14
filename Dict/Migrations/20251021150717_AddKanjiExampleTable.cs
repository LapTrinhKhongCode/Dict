using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class AddKanjiExampleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<string>(
            //    name: "AvatarUrl",
            //    table: "users",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<string>(
            //    name: "Role",
            //    table: "users",
            //    type: "nvarchar(64)",
            //    maxLength: 64,
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AlterColumn<string>(
            //    name: "Template",
            //    table: "cards",
            //    type: "nvarchar(max)",
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(max)");

            //migrationBuilder.CreateTable(
            //    name: "KanjiExample",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        KanjiId = table.Column<int>(type: "int", nullable: false),
            //        ExampleType = table.Column<string>(type: "nvarchar(50)", nullable: false),
            //        ReadingGroup = table.Column<string>(type: "nvarchar(255)", nullable: true),
            //        Word = table.Column<string>(type: "nvarchar(255)", nullable: false),
            //        Meaning = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        Reading = table.Column<string>(type: "nvarchar(255)", nullable: false),
            //        HanViet = table.Column<string>(type: "nvarchar(255)", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_KanjiExample", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_KanjiExample_kanji_KanjiId",
            //            column: x => x.KanjiId,
            //            principalTable: "kanji",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_KanjiExample_KanjiId",
            //    table: "KanjiExample",
            //    column: "KanjiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KanjiExample");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "Template",
                table: "cards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
