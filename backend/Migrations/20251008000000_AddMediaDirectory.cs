using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlashcardApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaDirectory",
                table: "Decks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaDirectory",
                table: "Decks");
        }
    }
}
