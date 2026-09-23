using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Api.Data.Migrations
{
    public partial class EnableRowLevelSecurity : Migration
    {
        private static readonly string[] Tables =
        [
            "items",
            "uoms",
            "item_uoms",
            "stock",
            "reservations",
            "reservation_lines",
            "__EFMigrationsHistory"
        ];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" DISABLE ROW LEVEL SECURITY;");
        }
    }
}
