using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStatsFreqToEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stats_word_freq_words_WordId",
                table: "stats_word_freq");
            migrationBuilder.Sql("DELETE FROM [stats_word_freq]");
            migrationBuilder.RenameColumn(
                name: "WordId",
                table: "stats_word_freq",
                newName: "EntryId");

            migrationBuilder.RenameIndex(
                name: "IX_stats_word_freq_WordId",
                table: "stats_word_freq",
                newName: "IX_stats_word_freq_EntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_word_freq_entries_EntryId",
                table: "stats_word_freq",
                column: "EntryId",
                principalTable: "entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stats_word_freq_entries_EntryId",
                table: "stats_word_freq");

            migrationBuilder.RenameColumn(
                name: "EntryId",
                table: "stats_word_freq",
                newName: "WordId");

            migrationBuilder.RenameIndex(
                name: "IX_stats_word_freq_EntryId",
                table: "stats_word_freq",
                newName: "IX_stats_word_freq_WordId");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_word_freq_words_WordId",
                table: "stats_word_freq",
                column: "WordId",
                principalTable: "words",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
