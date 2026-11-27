using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
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
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    chunk_index = table.Column<int>(type: "integer", nullable: false),
                    format = table.Column<byte>(type: "smallint", nullable: false),
                    log_count = table.Column<int>(type: "integer", nullable: false),
                    first_log_id = table.Column<int>(type: "integer", nullable: false),
                    last_log_id = table.Column<int>(type: "integer", nullable: false),
                    first_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    raw_size = table.Column<int>(type: "integer", nullable: false),
                    flags = table.Column<byte>(type: "smallint", nullable: false),
                    impact_mask = table.Column<byte>(type: "smallint", nullable: false),
                    type_mask = table.Column<byte[]>(type: "bytea", nullable: false),
                    players = table.Column<byte[]>(type: "bytea", nullable: false),
                    payload = table.Column<byte[]>(type: "bytea", nullable: false)
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
