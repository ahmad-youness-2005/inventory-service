using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Api.Data.Migrations
{
    public partial class MultiItemReservations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservation_lines",
                columns: table => new
                {
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservation_lines", x => new { x.reservation_id, x.item_id });
                    table.CheckConstraint("ck_reservation_lines_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_reservation_lines_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reservation_lines_reservations_reservation_id",
                        column: x => x.reservation_id,
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO reservation_lines (reservation_id, item_id, quantity)
                SELECT id, item_id, quantity FROM reservations;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_reservations_items_item_id",
                table: "reservations");

            migrationBuilder.DropIndex(
                name: "ix_reservations_item_id",
                table: "reservations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reservations_quantity_positive",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "item_id",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "reservations");

            migrationBuilder.DropIndex(
                name: "ix_reservations_order_reference",
                table: "reservations");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM reservations
                        WHERE status IN ('Pending', 'Confirmed')
                        GROUP BY order_reference
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Some orders have more than one Pending/Confirmed reservation. Release or confirm the Pending ones first, then run the migration again.';
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_reservations_order_reference",
                table: "reservations",
                column: "order_reference",
                unique: true,
                filter: "status IN ('Pending', 'Confirmed')");

            migrationBuilder.CreateIndex(
                name: "ix_reservation_lines_item_id",
                table: "reservation_lines",
                column: "item_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM reservation_lines GROUP BY reservation_id HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'Some reservations have more than one line and can''t be converted back.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "ix_reservations_order_reference",
                table: "reservations");

            migrationBuilder.AddColumn<Guid>(
                name: "item_id",
                table: "reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "reservations",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE reservations r
                SET item_id = l.item_id, quantity = l.quantity
                FROM reservation_lines l
                WHERE l.reservation_id = r.id;

                DELETE FROM reservations WHERE item_id IS NULL;
                ALTER TABLE reservations ALTER COLUMN item_id SET NOT NULL;
                ALTER TABLE reservations ALTER COLUMN quantity SET NOT NULL;
                """);

            migrationBuilder.DropTable(
                name: "reservation_lines");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reservations_quantity_positive",
                table: "reservations",
                sql: "quantity > 0");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_item_id",
                table: "reservations",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_order_reference",
                table: "reservations",
                column: "order_reference");

            migrationBuilder.AddForeignKey(
                name: "fk_reservations_items_item_id",
                table: "reservations",
                column: "item_id",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
