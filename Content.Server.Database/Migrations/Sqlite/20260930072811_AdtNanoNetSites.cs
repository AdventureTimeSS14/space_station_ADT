using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdtNanoNetSites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adt_nano_net_site",
                columns: table => new
                {
                    adt_nano_net_site_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    label = table.Column<string>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    owner_name = table.Column<string>(type: "TEXT", nullable: false),
                    html = table.Column<string>(type: "TEXT", nullable: false),
                    published_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_nano_net_site", x => x.adt_nano_net_site_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adt_nano_net_site_label",
                table: "adt_nano_net_site",
                column: "label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adt_nano_net_site_user_id",
                table: "adt_nano_net_site",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adt_nano_net_site");
        }
    }
}
