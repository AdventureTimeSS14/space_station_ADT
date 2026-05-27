using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class unknown_migrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "server_role_unban");

            migrationBuilder.DropTable(
                name: "server_unban");

            migrationBuilder.DropTable(
                name: "server_role_ban");

            migrationBuilder.DropTable(
                name: "ServerBan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "server_role_ban",
                columns: table => new
                {
                    server_role_ban_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    banning_admin = table.Column<Guid>(type: "uuid", nullable: true),
                    last_edited_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    round_id = table.Column<int>(type: "integer", nullable: true),
                    address = table.Column<NpgsqlInet>(type: "inet", nullable: true),
                    ban_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hidden = table.Column<bool>(type: "boolean", nullable: false),
                    last_edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    player_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    playtime_at_note = table.Column<TimeSpan>(type: "interval", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    hwid = table.Column<byte[]>(type: "bytea", nullable: true),
                    hwid_type = table.Column<int>(type: "integer", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_role_ban", x => x.server_role_ban_id);
                    table.CheckConstraint("AddressNotIPv6MappedIPv4", "NOT inet '::ffff:0.0.0.0/96' >>= address");
                    table.CheckConstraint("HaveEitherAddressOrUserIdOrHWId", "address IS NOT NULL OR player_user_id IS NOT NULL OR hwid IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_server_role_ban_player_banning_admin",
                        column: x => x.banning_admin,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_server_role_ban_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_server_role_ban_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateTable(
                name: "ServerBan",
                columns: table => new
                {
                    TempId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.UniqueConstraint("AK_ServerBan_TempId", x => x.TempId);
                });

            migrationBuilder.CreateTable(
                name: "server_role_unban",
                columns: table => new
                {
                    role_unban_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ban_id = table.Column<int>(type: "integer", nullable: false),
                    unban_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_role_unban", x => x.role_unban_id);
                    table.ForeignKey(
                        name: "FK_server_role_unban_server_role_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "server_role_ban",
                        principalColumn: "server_role_ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "server_unban",
                columns: table => new
                {
                    unban_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ban_id = table.Column<int>(type: "integer", nullable: false),
                    unban_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_unban", x => x.unban_id);
                    table.ForeignKey(
                        name: "FK_server_unban_server_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ServerBan",
                        principalColumn: "TempId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_address",
                table: "server_role_ban",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_banning_admin",
                table: "server_role_ban",
                column: "banning_admin");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_last_edited_by_id",
                table: "server_role_ban",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_player_user_id",
                table: "server_role_ban",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_round_id",
                table: "server_role_ban",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_unban_ban_id",
                table: "server_role_unban",
                column: "ban_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_server_unban_ban_id",
                table: "server_unban",
                column: "ban_id",
                unique: true);
        }
    }
}
