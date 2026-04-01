using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectToMediaStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "media_store",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_store_ProjectId",
                table: "media_store",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_media_store_projects_ProjectId",
                table: "media_store",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_media_store_projects_ProjectId",
                table: "media_store");

            migrationBuilder.DropIndex(
                name: "IX_media_store_ProjectId",
                table: "media_store");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "media_store");
        }
    }
}
