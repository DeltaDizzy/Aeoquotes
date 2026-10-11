using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aeoquotes.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nick = table.Column<string>(type: "TEXT", nullable: false),
                    userId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    channel = table.Column<string>(type: "TEXT", nullable: false),
                    channelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    server = table.Column<ulong>(type: "INTEGER", nullable: false),
                    text = table.Column<string>(type: "TEXT", nullable: false),
                    messageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    unixTime = table.Column<long>(type: "INTEGER", nullable: false),
                    dateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quotes");
        }
    }
}
