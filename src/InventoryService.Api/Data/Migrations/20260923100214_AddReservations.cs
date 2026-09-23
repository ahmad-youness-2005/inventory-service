using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Api.Data.Migrations
{
    public partial class AddReservations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reserved",
                table: "stock",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservations", x => x.id);
                    table.CheckConstraint("ck_reservations_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_reservations_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_reserved_not_above_quantity",
                table: "stock",
                sql: "reserved <= quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_reserved_not_negative",
                table: "stock",
                sql: "reserved >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_item_id",
                table: "reservations",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_order_reference",
                table: "reservations",
                column: "order_reference");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_status_expires_at",
                table: "reservations",
                columns: new[] { "status", "expires_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_reserved_not_above_quantity",
                table: "stock");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_reserved_not_negative",
                table: "stock");

            migrationBuilder.DropColumn(
                name: "reserved",
                table: "stock");
        }
    }
}
