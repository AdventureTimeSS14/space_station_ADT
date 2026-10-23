using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
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
                    adt_poll_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    questions = table.Column<string>(type: "text", nullable: false),
                    filters = table.Column<string>(type: "text", nullable: false),
                    servers = table.Column<string>(type: "text", nullable: false),
                    show_mode = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_poll", x => x.adt_poll_id);
                });

            migrationBuilder.CreateTable(
                name: "adt_poll_pending",
                columns: table => new
                {
                    poll_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    adt_poll_response_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    poll_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: true),
                    server = table.Column<string>(type: "text", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    targeted = table.Column<bool>(type: "boolean", nullable: false),
                    snapshot = table.Column<string>(type: "text", nullable: false),
                    answers = table.Column<string>(type: "text", nullable: false)
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
