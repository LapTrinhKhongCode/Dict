using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Tạo Full-Text Catalog (Thêm suppressTransaction: true) ---
            migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'DictFtCatalog')
            CREATE FULLTEXT CATALOG DictFtCatalog AS DEFAULT;
        ", suppressTransaction: true); // <-- THÊM VÀO ĐÂY

            // --- Tạo Full-Text Index (Thêm suppressTransaction: true cho an toàn) ---
            migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.entries'))
            CREATE FULLTEXT INDEX ON dbo.entries(
                Label LANGUAGE 1041,
                Phonetic LANGUAGE 1041
            )
            KEY INDEX PK_entries -- Đảm bảo đây là tên khóa chính đúng
            ON DictFtCatalog;
        ", suppressTransaction: true); // <-- THÊM VÀO ĐÂY (tùy chọn nhưng nên làm)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tương tự, có thể thêm suppressTransaction cho các lệnh DROP nếu cần
            migrationBuilder.Sql(@"
            IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.entries'))
            DROP FULLTEXT INDEX ON dbo.entries;
        ", suppressTransaction: true); // <-- Thêm vào đây

            migrationBuilder.Sql(@"
             IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'DictFtCatalog')
             AND NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes fti JOIN sys.fulltext_catalogs ftc ON fti.fulltext_catalog_id = ftc.fulltext_catalog_id WHERE ftc.name = 'DictFtCatalog')
             DROP FULLTEXT CATALOG DictFtCatalog;
        ", suppressTransaction: true); // <-- Thêm vào đây
        }
    }
}
