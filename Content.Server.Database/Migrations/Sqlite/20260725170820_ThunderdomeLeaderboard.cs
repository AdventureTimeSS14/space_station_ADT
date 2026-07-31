using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ThunderdomeLeaderboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "thunderdome_stats",
                columns: table => new
                {
                    thunderdome_stats_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    kills = table.Column<int>(type: "INTEGER", nullable: false),
                    deaths = table.Column<int>(type: "INTEGER", nullable: false),
                    score = table.Column<float>(type: "REAL", nullable: false),
                    best_streak = table.Column<int>(type: "INTEGER", nullable: false),
                    rounds_played = table.Column<int>(type: "INTEGER", nullable: false),
                    last_played = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thunderdome_stats", x => x.thunderdome_stats_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_thunderdome_stats_score",
                table: "thunderdome_stats",
                column: "score",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_thunderdome_stats_user_id",
                table: "thunderdome_stats",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "thunderdome_stats");
        }
    }
}
