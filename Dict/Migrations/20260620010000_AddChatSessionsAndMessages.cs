using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    public partial class AddChatSessionsAndMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_sessions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    ScopeType = table.Column<string>(maxLength: 20, nullable: false),
                    ScopeId = table.Column<int>(nullable: false),
                    Title = table.Column<string>(maxLength: 200, nullable: false, defaultValue: "Hội thoại mới"),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ChatSessionId = table.Column<int>(nullable: false),
                    Role = table.Column<string>(maxLength: 20, nullable: false),
                    Content = table.Column<string>(nullable: false),
                    SourcesJson = table.Column<string>(nullable: true),
                    CitationsJson = table.Column<string>(nullable: true),
                    CacheHit = table.Column<bool>(nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey("FK_chat_messages_chat_sessions", x => x.ChatSessionId, "chat_sessions", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_chat_sessions_UserId_ScopeType_ScopeId", "chat_sessions", new[] { "UserId", "ScopeType", "ScopeId" });
            migrationBuilder.CreateIndex("IX_chat_messages_ChatSessionId", "chat_messages", "ChatSessionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("chat_messages");
            migrationBuilder.DropTable("chat_sessions");
        }
    }
}
