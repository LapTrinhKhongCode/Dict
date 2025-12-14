using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class AddShortMeanToEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<string>(
            //    name: "ShortMean",
            //    table: "entries",
            //    type: "nvarchar(255)",
            //    maxLength: 255,
            //    nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortMean",
                table: "entries");
        }
    }
}
