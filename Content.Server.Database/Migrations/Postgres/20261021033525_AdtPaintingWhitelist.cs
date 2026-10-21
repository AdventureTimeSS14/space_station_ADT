using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
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
                    report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    painting_uid = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: true),
                    server_id = table.Column<int>(type: "integer", nullable: true),
                    resolved = table.Column<bool>(type: "boolean", nullable: false),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
