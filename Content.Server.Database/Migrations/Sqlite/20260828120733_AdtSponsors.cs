using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdtSponsors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adt_sponsor_preference",
                columns: table => new
                {
                    adt_sponsor_preference_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ooc_color = table.Column<string>(type: "TEXT", nullable: true),
                    ghost_color = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_sponsor_preference", x => x.adt_sponsor_preference_id);
                });

            migrationBuilder.CreateTable(
                name: "adt_sponsor_tier",
                columns: table => new
                {
                    adt_sponsor_tier_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    display_name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    priority = table.Column<int>(type: "INTEGER", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    benefits = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_sponsor_tier", x => x.adt_sponsor_tier_id);
                });

            migrationBuilder.CreateTable(
                name: "adt_sponsor_grant",
                columns: table => new
                {
                    adt_sponsor_grant_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tier_id = table.Column<int>(type: "INTEGER", nullable: true),
                    priority = table.Column<int>(type: "INTEGER", nullable: false),
                    overrides = table.Column<string>(type: "TEXT", nullable: true),
                    comment = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    revoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    revoked_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_sponsor_grant", x => x.adt_sponsor_grant_id);
                    table.ForeignKey(
                        name: "FK_adt_sponsor_grant_adt_sponsor_tier_tier_id",
                        column: x => x.tier_id,
                        principalTable: "adt_sponsor_tier",
                        principalColumn: "adt_sponsor_tier_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adt_sponsor_grant_tier_id",
                table: "adt_sponsor_grant",
                column: "tier_id");

            migrationBuilder.CreateIndex(
                name: "IX_adt_sponsor_grant_user_id_revoked",
                table: "adt_sponsor_grant",
                columns: new[] { "user_id", "revoked" });

            migrationBuilder.CreateIndex(
                name: "IX_adt_sponsor_preference_user_id",
                table: "adt_sponsor_preference",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adt_sponsor_tier_name",
                table: "adt_sponsor_tier",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adt_sponsor_grant");

            migrationBuilder.DropTable(
                name: "adt_sponsor_preference");

            migrationBuilder.DropTable(
                name: "adt_sponsor_tier");
        }
    }
}
