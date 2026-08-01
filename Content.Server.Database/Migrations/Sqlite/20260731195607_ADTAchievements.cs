using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ADTAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adtplayer_achievement",
                columns: table => new
                {
                    adtplayer_achievement_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    achievement_id = table.Column<string>(type: "TEXT", nullable: false),
                    progress = table.Column<int>(type: "INTEGER", nullable: false),
                    unlocked_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adtplayer_achievement", x => x.adtplayer_achievement_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adtplayer_achievement_user_id_achievement_id",
                table: "adtplayer_achievement",
                columns: new[] { "user_id", "achievement_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adtplayer_achievement");
        }
    }
}
