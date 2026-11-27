using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdtAdminLogChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adt_admin_log_chunk",
                columns: table => new
                {
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    chunk_index = table.Column<int>(type: "INTEGER", nullable: false),
                    format = table.Column<byte>(type: "INTEGER", nullable: false),
                    log_count = table.Column<int>(type: "INTEGER", nullable: false),
                    first_log_id = table.Column<int>(type: "INTEGER", nullable: false),
                    last_log_id = table.Column<int>(type: "INTEGER", nullable: false),
                    first_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    raw_size = table.Column<int>(type: "INTEGER", nullable: false),
                    flags = table.Column<byte>(type: "INTEGER", nullable: false),
                    impact_mask = table.Column<byte>(type: "INTEGER", nullable: false),
                    type_mask = table.Column<byte[]>(type: "BLOB", nullable: false),
                    players = table.Column<byte[]>(type: "BLOB", nullable: false),
                    payload = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adt_admin_log_chunk", x => new { x.round_id, x.chunk_index });
                });

            migrationBuilder.CreateIndex(
                name: "IX_adt_admin_log_chunk_first_date",
                table: "adt_admin_log_chunk",
                column: "first_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adt_admin_log_chunk");
        }
    }
}
