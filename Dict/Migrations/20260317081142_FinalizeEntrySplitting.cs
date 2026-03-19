using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeEntrySplitting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Dọn dẹp Index cũ (Dùng SQL check cho chắc)
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_entries_Type_Label' AND object_id = OBJECT_ID('entries')) DROP INDEX [IX_entries_Type_Label] ON [entries]");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_entries_Type_Phonetic' AND object_id = OBJECT_ID('entries')) DROP INDEX [IX_entries_Type_Phonetic] ON [entries]");

            // 2. Hạ độ dài Label (Check trước khi Alter)
            migrationBuilder.Sql(@"
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('entries') AND name = 'Label' AND max_length > 900)
        BEGIN
            ALTER TABLE [entries] ALTER COLUMN [Label] nvarchar(450) NOT NULL;
        END");

            // 3. Thêm cột ShortMean vào bảng Gầy (Nếu chưa có)
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('entries') AND name = 'ShortMean') ALTER TABLE [entries] ADD [ShortMean] nvarchar(450) NULL");

            // 4. Đồng bộ dữ liệu (Chỉ chạy nếu bảng Béo vẫn còn cột ShortMean)
            migrationBuilder.Sql(@"
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('entry_details') AND name = 'ShortMean')
        BEGIN
            UPDATE e
            SET e.ShortMean = CAST(d.ShortMean AS nvarchar(450))
            FROM entries e
            INNER JOIN entry_details d ON e.Id = d.Id
            WHERE d.ShortMean IS NOT NULL;
            
            -- Sau khi copy xong thì xóa ở bảng Béo luôn cho sạch
            ALTER TABLE [entry_details] DROP COLUMN [ShortMean];
        END");

            // 5. Cập nhật JsonProcessingStatus
            migrationBuilder.Sql("ALTER TABLE [entry_details] ALTER COLUMN [JsonProcessingStatus] nvarchar(max) NULL");

            // 6. Xử lý cột "Cản địa" SynsetProcessingStatus (BƯỚC GÂY LỖI NÈ)
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('entry_details') AND name = 'SynsetProcessingStatus') ALTER TABLE [entry_details] ADD [SynsetProcessingStatus] nvarchar(max) NULL");

            // 7. Tạo Index thần thánh (Xóa cái cũ nếu lỡ tạo dở dang rồi tạo mới)
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_entries_SmartSearch' AND object_id = OBJECT_ID('entries')) DROP INDEX [IX_entries_SmartSearch] ON [entries]");

            migrationBuilder.CreateIndex(
                name: "IX_entries_SmartSearch",
                table: "entries",
                column: "Label")
                .Annotation("SqlServer:Include", new[] { "Phonetic", "Type", "Weight", "ShortMean" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortMean",
                table: "entry_details",
                type: "varchar(max)",
                unicode: false,
                nullable: true,
                collation: "Latin1_General_100_CI_AS_SC_UTF8");

            migrationBuilder.Sql(@"
                UPDATE d
                SET d.ShortMean = CAST(e.ShortMean AS varchar(max))
                FROM entry_details d
                INNER JOIN entries e ON d.Id = e.Id
            ");

            migrationBuilder.DropIndex(name: "IX_entries_SmartSearch", table: "entries");
            migrationBuilder.DropColumn(name: "ShortMean", table: "entries");
            migrationBuilder.DropColumn(name: "SynsetProcessingStatus", table: "entry_details");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "entries",
                type: "nvarchar(900)",
                maxLength: 900,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_entries_Type_Label",
                table: "entries",
                columns: new[] { "Type", "Label" });
        }
    }
}
