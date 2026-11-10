using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddAdtMapRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adt_map_rotation",
                columns: table => new
                {
                    server = table.Column<string>(type: "TEXT", nullable: false),
                    map_id = table.Column<string>(type: "TEXT", nullable: false),
                    bonus_per_player = table.Column<double>(type: "REAL", nullable: false),
                    last_played = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_map_rotation", x => new { x.server, x.map_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adt_map_rotation");
        }
    }
}
