using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class AddOcrIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Composite index cho query AnyAsync(OcrJobId, PageNumber) — dùng nhiều trong UploadAndOcrPageAsync
            migrationBuilder.CreateIndex(
                name: "IX_ocr_results_JobId_PageNumber",
                table: "ocr_results",
                columns: new[] { "OcrJobId", "PageNumber" });

            // Index cho query GetRecentOcrJobsForUserAsync — WHERE UserId ORDER BY CreatedAt DESC
            migrationBuilder.CreateIndex(
                name: "IX_ocr_jobs_UserId_CreatedAt",
                table: "ocr_jobs",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ocr_results_JobId_PageNumber",
                table: "ocr_results");

            migrationBuilder.DropIndex(
                name: "IX_ocr_jobs_UserId_CreatedAt",
                table: "ocr_jobs");
        }
    }
}
