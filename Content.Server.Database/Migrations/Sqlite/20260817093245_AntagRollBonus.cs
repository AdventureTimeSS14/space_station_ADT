using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AntagRollBonus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "antag_roll_bonus",
                columns: table => new
                {
                    antag_roll_bonus_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    antag = table.Column<string>(type: "TEXT", nullable: false),
                    missed_rounds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antag_roll_bonus", x => x.antag_roll_bonus_id);
                });

            migrationBuilder.CreateTable(
                name: "antag_roll_bonus_wipe",
                columns: table => new
                {
                    antag_roll_bonus_wipe_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    last_wipe = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antag_roll_bonus_wipe", x => x.antag_roll_bonus_wipe_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_antag_roll_bonus_user_id_antag",
                table: "antag_roll_bonus",
                columns: new[] { "user_id", "antag" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "antag_roll_bonus");

            migrationBuilder.DropTable(
                name: "antag_roll_bonus_wipe");
        }
    }
}
