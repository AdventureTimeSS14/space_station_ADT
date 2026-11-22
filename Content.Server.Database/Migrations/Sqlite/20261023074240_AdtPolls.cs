using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdtPolls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adt_poll",
                columns: table => new
                {
                    adt_poll_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    questions = table.Column<string>(type: "TEXT", nullable: false),
                    filters = table.Column<string>(type: "TEXT", nullable: false),
                    servers = table.Column<string>(type: "TEXT", nullable: false),
                    show_mode = table.Column<int>(type: "INTEGER", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ends_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    closed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_poll", x => x.adt_poll_id);
                });

            migrationBuilder.CreateTable(
                name: "adt_poll_pending",
                columns: table => new
                {
                    poll_id = table.Column<int>(type: "INTEGER", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_poll_pending", x => new { x.poll_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_adt_poll_pending_adt_poll_poll_id",
                        column: x => x.poll_id,
                        principalTable: "adt_poll",
                        principalColumn: "adt_poll_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "adt_poll_response",
                columns: table => new
                {
                    adt_poll_response_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    poll_id = table.Column<int>(type: "INTEGER", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    server = table.Column<string>(type: "TEXT", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    targeted = table.Column<bool>(type: "INTEGER", nullable: false),
                    snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    answers = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_poll_response", x => x.adt_poll_response_id);
                    table.ForeignKey(
                        name: "FK_adt_poll_response_adt_poll_poll_id",
                        column: x => x.poll_id,
                        principalTable: "adt_poll",
                        principalColumn: "adt_poll_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adt_poll_pending_user_id",
                table: "adt_poll_pending",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_adt_poll_response_poll_id_user_id",
                table: "adt_poll_response",
                columns: new[] { "poll_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adt_poll_response_user_id",
                table: "adt_poll_response",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adt_poll_pending");

            migrationBuilder.DropTable(
                name: "adt_poll_response");

            migrationBuilder.DropTable(
                name: "adt_poll");
        }
    }
}
