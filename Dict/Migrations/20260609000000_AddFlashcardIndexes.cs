using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashcardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Composite index cho query phổ biến nhất: ProcessAnswerAsync + GetReviewQueueAsync
            // WHERE UserId = X AND CardId = Y
            migrationBuilder.CreateIndex(
                name: "IX_card_states_UserId_CardId",
                table: "card_states",
                columns: new[] { "UserId", "CardId" },
                unique: true); // Mỗi (user, card) chỉ có 1 state

            // Index cho GetReviewQueueAsync: WHERE UserId = X AND DueDate <= now
            migrationBuilder.CreateIndex(
                name: "IX_card_states_UserId_DueDate",
                table: "card_states",
                columns: new[] { "UserId", "DueDate" });

            // Index cho search deck theo tên (hỗ trợ LIKE 'query%')
            migrationBuilder.CreateIndex(
                name: "IX_decks_Name",
                table: "decks",
                column: "Name");

            // Index hỗ trợ GetPublicDecksAsync và SearchPublicDecksByNameAsync
            migrationBuilder.CreateIndex(
                name: "IX_decks_IsPublic_UpdatedAt",
                table: "decks",
                columns: new[] { "IsPublic", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_card_states_UserId_CardId", table: "card_states");
            migrationBuilder.DropIndex(name: "IX_card_states_UserId_DueDate", table: "card_states");
            migrationBuilder.DropIndex(name: "IX_decks_Name", table: "decks");
            migrationBuilder.DropIndex(name: "IX_decks_IsPublic_UpdatedAt", table: "decks");
        }
    }
}
