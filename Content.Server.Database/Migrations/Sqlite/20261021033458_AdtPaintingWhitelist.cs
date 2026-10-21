using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdtPaintingWhitelist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adt_painting_report",
                columns: table => new
                {
                    report_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    painting_uid = table.Column<Guid>(type: "TEXT", nullable: false),
                    reporter_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    reason = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: true),
                    resolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    resolved_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_painting_report", x => x.report_id);
                    table.ForeignKey(
                        name: "FK_adt_painting_report_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_adt_painting_report_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "adt_painting_whitelist",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    added_by = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_painting_whitelist", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_report_painting_uid_reporter_user_id",
                table: "adt_painting_report",
                columns: new[] { "painting_uid", "reporter_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_report_resolved_created_at",
                table: "adt_painting_report",
                columns: new[] { "resolved", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_report_round_id",
                table: "adt_painting_report",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_report_server_id",
                table: "adt_painting_report",
                column: "server_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adt_painting_report");

            migrationBuilder.DropTable(
                name: "adt_painting_whitelist");
        }
    }
}
