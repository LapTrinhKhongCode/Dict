using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class SplitEntryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Tạo bảng details nếu chưa có (Idempotent)
            migrationBuilder.Sql(@"
        IF OBJECT_ID('entry_details', 'U') IS NULL
        BEGIN
            CREATE TABLE [entry_details] (
                [Id] int NOT NULL,
                [RawJson] varchar(max) COLLATE Latin1_General_100_CI_AS_SC_UTF8 NOT NULL,
                [JsonProcessingStatus] nvarchar(100) NULL,
                [CommentRawJson] varchar(max) COLLATE Latin1_General_100_CI_AS_SC_UTF8 NOT NULL,
                [JsonErrorMessage] nvarchar(max) NULL,
                [ShortMean] varchar(max) COLLATE Latin1_General_100_CI_AS_SC_UTF8 NULL,
                [SynsetProcessingStatus] nvarchar(100) NULL,
                CONSTRAINT [PK_entry_details] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_entry_details_entries_Id] FOREIGN KEY ([Id]) REFERENCES [entries] ([Id]) ON DELETE CASCADE
            );
        END
    ");

            // 2. Chuyển dữ liệu (Chỉ chạy nếu bảng mới đang trống)
            migrationBuilder.Sql(@"
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('entries') AND name = 'RawJson')
           AND NOT EXISTS (SELECT 1 FROM entry_details)
        BEGIN
            INSERT INTO entry_details (Id, RawJson, JsonProcessingStatus, CommentRawJson, JsonErrorMessage, ShortMean, SynsetProcessingStatus)
            SELECT Id, RawJson, JsonProcessingStatus, CommentRawJson, JsonErrorMessage, ShortMean, SynsetProcessingStatus 
            FROM entries;
        END
    ");

            // 3. Xóa Index cũ (Idempotent)
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_entries_JsonProcessingStatus' AND object_id = OBJECT_ID('entries')) DROP INDEX [IX_entries_JsonProcessingStatus] ON [entries]");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_entries_Type_Label' AND object_id = OBJECT_ID('entries')) DROP INDEX [IX_entries_Type_Label] ON [entries]");

            // 4. BƯỚC QUYẾT ĐỊNH: Tự tìm Default Constraint và Xóa cột
            // Đoạn này sẽ chạy cho tất cả các cột cần xóa
            string[] colsToDrop = { "RawJson", "CommentRawJson", "ShortMean", "JsonProcessingStatus", "JsonErrorMessage", "SynsetProcessingStatus" };
            foreach (var col in colsToDrop)
            {
                migrationBuilder.Sql($@"
            DECLARE @var{col} sysname;
            SELECT @var{col} = [d].[name]
            FROM [sys].[default_constraints] [d]
            INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
            WHERE ([d].[parent_object_id] = OBJECT_ID(N'[entries]') AND [c].[name] = N'{col}');
            IF @var{col} IS NOT NULL EXEC(N'ALTER TABLE [entries] DROP CONSTRAINT [' + @var{col} + '];');
            
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('entries') AND name = '{col}')
                ALTER TABLE [entries] DROP COLUMN [{col}];
        ");
            }

            // 5. Cập nhật Label & Index (Idempotent)
            migrationBuilder.Sql(@"
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('entries') AND name = 'Label')
        BEGIN
            ALTER TABLE [entries] ALTER COLUMN [Label] nvarchar(900) NOT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_entries_Type_Label')
                CREATE INDEX [IX_entries_Type_Label] ON [entries] ([Type], [Label]);
        END
    ");

            // 6. Xử lý Foreign Keys (Idempotent)
            string[] tables = { "kanji_elements", "media", "reading_elements", "senses", "stats_word_freq", "translations", "words", "WordToEntries" };
            foreach (var table in tables)
            {
                migrationBuilder.Sql($"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_{table}_entries_EntryId') ALTER TABLE [{table}] DROP CONSTRAINT [FK_{table}_entries_EntryId]");

                string action = (table == "words") ? "SET NULL" : "CASCADE";
                migrationBuilder.Sql($@"
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_{table}_entries_EntryId')
            ALTER TABLE [{table}] ADD CONSTRAINT [FK_{table}_entries_EntryId] 
            FOREIGN KEY ([EntryId]) REFERENCES [entries] ([Id]) ON DELETE {action}");
            }
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Thêm lại các cột vào bảng entries
            migrationBuilder.AddColumn<string>(name: "RawJson", table: "entries", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "CommentRawJson", table: "entries", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "ShortMean", table: "entries", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "JsonProcessingStatus", table: "entries", type: "nvarchar(50)", maxLength: 50, nullable: true);
            migrationBuilder.AddColumn<string>(name: "JsonErrorMessage", table: "entries", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SynsetProcessingStatus", table: "entries", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.CreateIndex(
        name: "IX_entries_JsonProcessingStatus",
        table: "entries",
        column: "JsonProcessingStatus");
            // 2. Chuyển dữ liệu ngược lại
            migrationBuilder.Sql(@"
                UPDATE e
                SET e.RawJson = d.RawJson, 
                    e.CommentRawJson = d.CommentRawJson, 
                    e.ShortMean = d.ShortMean,
                    e.JsonProcessingStatus = LEFT(d.JsonProcessingStatus, 50),
                    e.JsonErrorMessage = d.JsonErrorMessage,
                    e.SynsetProcessingStatus = d.SynsetProcessingStatus
                FROM entries e
                INNER JOIN entry_details d ON e.Id = d.Id
            ");

            // 3. Xóa bảng phụ
            migrationBuilder.DropTable(name: "entry_details");

            // 4. Khôi phục lại độ dài cũ của Label (nếu muốn)
            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "entries",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(900)",
                oldMaxLength: 900);
        }
    }
}
