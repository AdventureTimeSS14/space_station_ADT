using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdtCanvas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adt_painting",
                columns: table => new
                {
                    painting_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    painting_uid = table.Column<Guid>(type: "TEXT", nullable: false),
                    parent_painting_id = table.Column<int>(type: "INTEGER", nullable: true),
                    owner_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    width = table.Column<int>(type: "INTEGER", nullable: false),
                    height = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<short>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reviewer_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    review_comment = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    author_notified = table.Column<bool>(type: "INTEGER", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: true),
                    head_revision_id = table.Column<int>(type: "INTEGER", nullable: true),
                    thumb_hash = table.Column<byte[]>(type: "BLOB", nullable: true),
                    bytes_total = table.Column<long>(type: "INTEGER", nullable: false),
                    pinned = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_painting", x => x.painting_id);
                    table.ForeignKey(
                        name: "FK_adt_painting_adt_painting_parent_painting_id",
                        column: x => x.parent_painting_id,
                        principalTable: "adt_painting",
                        principalColumn: "painting_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_adt_painting_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_adt_painting_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "adt_painting_ban",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    banned_by = table.Column<Guid>(type: "TEXT", nullable: false),
                    reason = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_painting_ban", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "adt_painting_blob",
                columns: table => new
                {
                    hash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    size = table.Column<int>(type: "INTEGER", nullable: false),
                    data = table.Column<byte[]>(type: "BLOB", nullable: true),
                    location = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_painting_blob", x => x.hash);
                });

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

            migrationBuilder.CreateTable(
                name: "adt_painting_contributor",
                columns: table => new
                {
                    painting_id = table.Column<int>(type: "INTEGER", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    role = table.Column<short>(type: "INTEGER", nullable: false),
                    tiles_committed = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_painting_contributor", x => new { x.painting_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_adt_painting_contributor_adt_painting_painting_id",
                        column: x => x.painting_id,
                        principalTable: "adt_painting",
                        principalColumn: "painting_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "adt_painting_revision",
                columns: table => new
                {
                    revision_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    painting_id = table.Column<int>(type: "INTEGER", nullable: false),
                    kind = table.Column<short>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    bytes_total = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_painting_revision", x => x.revision_id);
                    table.ForeignKey(
                        name: "FK_adt_painting_revision_adt_painting_painting_id",
                        column: x => x.painting_id,
                        principalTable: "adt_painting",
                        principalColumn: "painting_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "adt_painting_revision_tile",
                columns: table => new
                {
                    revision_id = table.Column<int>(type: "INTEGER", nullable: false),
                    layer = table.Column<short>(type: "INTEGER", nullable: false),
                    lod = table.Column<short>(type: "INTEGER", nullable: false),
                    tile_x = table.Column<short>(type: "INTEGER", nullable: false),
                    tile_y = table.Column<short>(type: "INTEGER", nullable: false),
                    kind = table.Column<short>(type: "INTEGER", nullable: false),
                    solid_rgba = table.Column<int>(type: "INTEGER", nullable: true),
                    blob_hash = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_painting_revision_tile", x => new { x.revision_id, x.layer, x.lod, x.tile_x, x.tile_y });
                    table.ForeignKey(
                        name: "FK_adt_painting_revision_tile_adt_painting_blob_adt_painting_blob_hash",
                        column: x => x.blob_hash,
                        principalTable: "adt_painting_blob",
                        principalColumn: "hash",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_adt_painting_revision_tile_adt_painting_revision_revision_id",
                        column: x => x.revision_id,
                        principalTable: "adt_painting_revision",
                        principalColumn: "revision_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_owner_user_id_status",
                table: "adt_painting",
                columns: new[] { "owner_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_painting_uid",
                table: "adt_painting",
                column: "painting_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_parent_painting_id",
                table: "adt_painting",
                column: "parent_painting_id");

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_round_id",
                table: "adt_painting",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_server_id",
                table: "adt_painting",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_status_reviewed_at",
                table: "adt_painting",
                columns: new[] { "status", "reviewed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_status_submitted_at",
                table: "adt_painting",
                columns: new[] { "status", "submitted_at" });

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

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_revision_painting_id_kind",
                table: "adt_painting_revision",
                columns: new[] { "painting_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "IX_adt_painting_revision_tile_blob_hash",
                table: "adt_painting_revision_tile",
                column: "blob_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adt_painting_ban");

            migrationBuilder.DropTable(
                name: "adt_painting_contributor");

            migrationBuilder.DropTable(
                name: "adt_painting_report");

            migrationBuilder.DropTable(
                name: "adt_painting_revision_tile");

            migrationBuilder.DropTable(
                name: "adt_painting_whitelist");

            migrationBuilder.DropTable(
                name: "adt_painting_blob");

            migrationBuilder.DropTable(
                name: "adt_painting_revision");

            migrationBuilder.DropTable(
                name: "adt_painting");
        }
    }
}
