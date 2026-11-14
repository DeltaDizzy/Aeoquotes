using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aeoquotes.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "imageFileName",
                table: "Quotes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "imageFileName",
                table: "Quotes");
        }
    }
}
