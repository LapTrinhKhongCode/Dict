using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    public partial class AddDocumentTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_tables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OcrJobId = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    TableIndex = table.Column<int>(type: "int", nullable: false),
                    SectionTitle = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    ColumnCount = table.Column<int>(type: "int", nullable: false),
                    HeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CellsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SummaryForEmbedding = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_tables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_tables_ocr_jobs_OcrJobId",
                        column: x => x.OcrJobId,
                        principalTable: "ocr_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_tables_ContentHash",
                table: "document_tables",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_document_tables_OcrJobId",
                table: "document_tables",
                column: "OcrJobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "document_tables");
        }
    }
}
