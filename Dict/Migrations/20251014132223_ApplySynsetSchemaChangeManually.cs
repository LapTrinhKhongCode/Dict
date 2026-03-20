using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class ApplySynsetSchemaChangeManually : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "synsets");

            //migrationBuilder.AlterColumn<string>(
            //    name: "Romaji",
            //    table: "entries",
            //    type: "nvarchar(255)",
            //    maxLength: 255,
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(255)",
            //    oldMaxLength: 255);

            //migrationBuilder.AlterColumn<string>(
            //    name: "Phonetic",
            //    table: "entries",
            //    type: "nvarchar(255)",
            //    maxLength: 255,
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(255)",
            //    oldMaxLength: 255);

            //migrationBuilder.AlterColumn<string>(
            //    name: "JsonProcessingStatus",
            //    table: "entries",
            //    type: "nvarchar(50)",
            //    maxLength: 50,
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(50)",
            //    oldMaxLength: 50);

            //migrationBuilder.AlterColumn<string>(
            //    name: "JsonErrorMessage",
            //    table: "entries",
            //    type: "nvarchar(max)",
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(max)");

            //migrationBuilder.CreateTable(
            //    name: "SynsetEntries",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        BaseForm = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //        Pos = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
            //        DefinitionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        SenseId = table.Column<int>(type: "int", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_SynsetEntries", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_SynsetEntries_senses_SenseId",
            //            column: x => x.SenseId,
            //            principalTable: "senses",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "SynonymItems",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        Word = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //        SynsetEntryId = table.Column<int>(type: "int", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_SynonymItems", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_SynonymItems_SynsetEntries_SynsetEntryId",
            //            column: x => x.SynsetEntryId,
            //            principalTable: "SynsetEntries",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_SynonymItems_SynsetEntryId",
            //    table: "SynonymItems",
            //    column: "SynsetEntryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_SynsetEntries_SenseId",
            //    table: "SynsetEntries",
            //    column: "SenseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "SynonymItems");

            //migrationBuilder.DropTable(
            //    name: "SynsetEntries");

            //migrationBuilder.AlterColumn<string>(
            //    name: "Romaji",
            //    table: "entries",
            //    type: "nvarchar(255)",
            //    maxLength: 255,
            //    nullable: false,
            //    defaultValue: "",
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(255)",
            //    oldMaxLength: 255,
            //    oldNullable: true);

            //migrationBuilder.AlterColumn<string>(
            //    name: "Phonetic",
            //    table: "entries",
            //    type: "nvarchar(255)",
            //    maxLength: 255,
            //    nullable: false,
            //    defaultValue: "",
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(255)",
            //    oldMaxLength: 255,
            //    oldNullable: true);

            //migrationBuilder.AlterColumn<string>(
            //    name: "JsonProcessingStatus",
            //    table: "entries",
            //    type: "nvarchar(50)",
            //    maxLength: 50,
            //    nullable: false,
            //    defaultValue: "",
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(50)",
            //    oldMaxLength: 50,
            //    oldNullable: true);

            //migrationBuilder.AlterColumn<string>(
            //    name: "JsonErrorMessage",
            //    table: "entries",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "",
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(max)",
            //    oldNullable: true);

            //migrationBuilder.CreateTable(
            //    name: "synsets",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        SenseId = table.Column<int>(type: "int", nullable: true),
            //        BaseForm = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //        Pos = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
            //        Synonyms = table.Column<string>(type: "nvarchar(max)", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_synsets", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_synsets_senses_SenseId",
            //            column: x => x.SenseId,
            //            principalTable: "senses",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_synsets_SenseId",
            //    table: "synsets",
            //    column: "SenseId");
        }
    }
}
