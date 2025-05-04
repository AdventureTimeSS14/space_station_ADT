using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class DiscordUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discord_player");

            migrationBuilder.CreateTable(
                name: "discord_user",
                columns: table => new
                {
                    discord_user_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    discord_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_user", x => x.discord_user_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_discord_user_user_id_discord_id",
                table: "discord_user",
                columns: new[] { "user_id", "discord_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discord_user");

            migrationBuilder.CreateTable(
                name: "discord_player",
                columns: table => new
                {
                    discord_player_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    discord_id = table.Column<int>(type: "INTEGER", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_player", x => x.discord_player_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_discord_player_user_id_discord_id",
                table: "discord_player",
                columns: new[] { "user_id", "discord_id" },
                unique: true);
        }
    }
}
