using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlashcardApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAnkiSchedulerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EaseFactor",
                table: "Cards",
                type: "REAL",
                nullable: false,
                defaultValue: 2.5);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStep",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScheduledSeconds",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ElapsedSeconds",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EaseFactor",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CurrentStep",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ScheduledSeconds",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ElapsedSeconds",
                table: "Cards");
        }
    }
}
