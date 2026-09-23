using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InventoryService.Api.Data.Migrations
{
    public partial class AddStockMovements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity_change = table.Column<int>(type: "integer", nullable: false),
                    reserved_change = table.Column<int>(type: "integer", nullable: false),
                    quantity_after = table.Column<int>(type: "integer", nullable: false),
                    reserved_after = table.Column<int>(type: "integer", nullable: false),
                    uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_quantity = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_reservations_reservation_id",
                        column: x => x.reservation_id,
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_uoms_uom_id",
                        column: x => x.uom_id,
                        principalTable: "uoms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_item_id_id",
                table: "stock_movements",
                columns: new[] { "item_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_reservation_id",
                table: "stock_movements",
                column: "reservation_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_uom_id",
                table: "stock_movements",
                column: "uom_id");

            migrationBuilder.Sql("ALTER TABLE \"stock_movements\" ENABLE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(
                """
                INSERT INTO stock_movements (item_id, type, quantity_change, reserved_change, quantity_after, reserved_after, reason, created_at)
                SELECT item_id, 'OpeningBalance', quantity, reserved, quantity, reserved, 'Stock before history was recorded', now()
                FROM stock
                WHERE quantity <> 0 OR reserved <> 0
                ORDER BY item_id;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_movements");
        }
    }
}
