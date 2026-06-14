using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SyncBanSnapshot : Migration
    {
        // ADT-Tweak: Эта миграция существует только чтобы привести снапшот модели в соответствие
        // с моделью после апстрим-мерджа BanRefactor

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
