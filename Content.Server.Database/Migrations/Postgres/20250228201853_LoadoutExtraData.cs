﻿using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class LoadoutExtraData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "extra_loadout_data",
                columns: table => new
                {
                    extra_loadout_data_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_role_loadout_id = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extra_loadout_data", x => x.extra_loadout_data_id);
                    table.ForeignKey(
                        name: "FK_extra_loadout_data_profile_role_loadout_profile_role_loadou~",
                        column: x => x.profile_role_loadout_id,
                        principalTable: "profile_role_loadout",
                        principalColumn: "profile_role_loadout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_extra_loadout_data_profile_role_loadout_id",
                table: "extra_loadout_data",
                column: "profile_role_loadout_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "extra_loadout_data");
        }
    }
}
