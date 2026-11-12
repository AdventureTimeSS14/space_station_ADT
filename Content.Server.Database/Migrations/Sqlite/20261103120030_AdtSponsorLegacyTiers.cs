using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdtSponsorLegacyTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adt_sponsor_legacy_tier",
                columns: table => new
                {
                    legacy_tier = table.Column<int>(type: "INTEGER", nullable: false),
                    tier_id = table.Column<int>(type: "INTEGER", nullable: true),
                    last_seen = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_sponsor_legacy_tier", x => x.legacy_tier);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adt_sponsor_legacy_tier");
        }
    }
}
