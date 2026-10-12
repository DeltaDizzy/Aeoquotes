using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aeoquotes.Migrations
{
    /// <inheritdoc />
    public partial class QuoterStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "quoterId",
                table: "Quotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<string>(
                name: "quoterUsername",
                table: "Quotes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quoterId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "quoterUsername",
                table: "Quotes");
        }
    }
}
