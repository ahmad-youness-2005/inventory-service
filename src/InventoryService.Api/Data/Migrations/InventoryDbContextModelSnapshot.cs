using System;
using InventoryService.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InventoryService.Api.Data.Migrations
{
    [DbContext(typeof(InventoryDbContext))]
    partial class InventoryDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("InventoryService.Api.Entities.Item", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("character varying(150)")
                        .HasColumnName("name");

                    b.Property<string>("Sku")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("sku");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id")
                        .HasName("pk_items");

                    b.HasIndex("Sku")
                        .IsUnique()
                        .HasDatabaseName("ix_items_sku");

                    b.ToTable("items", (string)null);
                });

            modelBuilder.Entity("InventoryService.Api.Entities.ItemUom", b =>
                {
                    b.Property<Guid>("ItemId")
                        .HasColumnType("uuid")
                        .HasColumnName("item_id");

                    b.Property<Guid>("UomId")
                        .HasColumnType("uuid")
                        .HasColumnName("uom_id");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<int>("QuantityPerUnit")
                        .HasColumnType("integer")
                        .HasColumnName("quantity_per_unit");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("now()");

                    b.HasKey("ItemId", "UomId")
                        .HasName("pk_item_uoms");

                    b.HasIndex("UomId")
                        .HasDatabaseName("ix_item_uoms_uom_id");

                    b.ToTable("item_uoms", null, t =>
                        {
                            t.HasCheckConstraint("ck_item_uoms_quantity_per_unit_positive", "quantity_per_unit > 0");
                        });
                });

            modelBuilder.Entity("InventoryService.Api.Entities.Reservation", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at");

                    b.Property<string>("OrderReference")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("order_reference");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("status");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id")
                        .HasName("pk_reservations");

                    b.HasIndex("OrderReference")
                        .IsUnique()
                        .HasDatabaseName("ix_reservations_order_reference")
                        .HasFilter("status IN ('Pending', 'Confirmed')");

                    b.HasIndex("Status", "ExpiresAt")
                        .HasDatabaseName("ix_reservations_status_expires_at");

                    b.ToTable("reservations", (string)null);
                });

            modelBuilder.Entity("InventoryService.Api.Entities.ReservationLine", b =>
                {
                    b.Property<Guid>("ReservationId")
                        .HasColumnType("uuid")
                        .HasColumnName("reservation_id");

                    b.Property<Guid>("ItemId")
                        .HasColumnType("uuid")
                        .HasColumnName("item_id");

                    b.Property<int>("Quantity")
                        .HasColumnType("integer")
                        .HasColumnName("quantity");

                    b.HasKey("ReservationId", "ItemId")
                        .HasName("pk_reservation_lines");

                    b.HasIndex("ItemId")
                        .HasDatabaseName("ix_reservation_lines_item_id");

                    b.ToTable("reservation_lines", null, t =>
                        {
                            t.HasCheckConstraint("ck_reservation_lines_quantity_positive", "quantity > 0");
                        });
                });

            modelBuilder.Entity("InventoryService.Api.Entities.Stock", b =>
                {
                    b.Property<Guid>("ItemId")
                        .HasColumnType("uuid")
                        .HasColumnName("item_id");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<int>("Quantity")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0)
                        .HasColumnName("quantity");

                    b.Property<int>("Reserved")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0)
                        .HasColumnName("reserved");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("now()");

                    b.HasKey("ItemId")
                        .HasName("pk_stock");

                    b.ToTable("stock", null, t =>
                        {
                            t.HasCheckConstraint("ck_stock_quantity_not_negative", "quantity >= 0");

                            t.HasCheckConstraint("ck_stock_reserved_not_above_quantity", "reserved <= quantity");

                            t.HasCheckConstraint("ck_stock_reserved_not_negative", "reserved >= 0");
                        });
                });

            modelBuilder.Entity("InventoryService.Api.Entities.StockMovement", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<Guid>("ItemId")
                        .HasColumnType("uuid")
                        .HasColumnName("item_id");

                    b.Property<int>("QuantityAfter")
                        .HasColumnType("integer")
                        .HasColumnName("quantity_after");

                    b.Property<int>("QuantityChange")
                        .HasColumnType("integer")
                        .HasColumnName("quantity_change");

                    b.Property<string>("Reason")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("reason");

                    b.Property<Guid?>("ReservationId")
                        .HasColumnType("uuid")
                        .HasColumnName("reservation_id");

                    b.Property<int>("ReservedAfter")
                        .HasColumnType("integer")
                        .HasColumnName("reserved_after");

                    b.Property<int>("ReservedChange")
                        .HasColumnType("integer")
                        .HasColumnName("reserved_change");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("type");

                    b.Property<int?>("UnitQuantity")
                        .HasColumnType("integer")
                        .HasColumnName("unit_quantity");

                    b.Property<Guid?>("UomId")
                        .HasColumnType("uuid")
                        .HasColumnName("uom_id");

                    b.HasKey("Id")
                        .HasName("pk_stock_movements");

                    b.HasIndex("ReservationId")
                        .HasDatabaseName("ix_stock_movements_reservation_id");

                    b.HasIndex("UomId")
                        .HasDatabaseName("ix_stock_movements_uom_id");

                    b.HasIndex("ItemId", "Id")
                        .HasDatabaseName("ix_stock_movements_item_id_id");

                    b.ToTable("stock_movements", (string)null);
                });

            modelBuilder.Entity("InventoryService.Api.Entities.Uom", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("Description")
                        .HasMaxLength(150)
                        .HasColumnType("character varying(150)")
                        .HasColumnName("description");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("name");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id")
                        .HasName("pk_uoms");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasDatabaseName("ix_uoms_name");

                    b.ToTable("uoms", (string)null);
                });

            modelBuilder.Entity("InventoryService.Api.Entities.ItemUom", b =>
                {
                    b.HasOne("InventoryService.Api.Entities.Item", "Item")
                        .WithMany("Units")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_item_uoms_items_item_id");

                    b.HasOne("InventoryService.Api.Entities.Uom", "Uom")
                        .WithMany("ItemUnits")
                        .HasForeignKey("UomId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_item_uoms_uoms_uom_id");

                    b.Navigation("Item");

                    b.Navigation("Uom");
                });

            modelBuilder.Entity("InventoryService.Api.Entities.ReservationLine", b =>
                {
                    b.HasOne("InventoryService.Api.Entities.Item", "Item")
                        .WithMany()
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_reservation_lines_items_item_id");

                    b.HasOne("InventoryService.Api.Entities.Reservation", "Reservation")
                        .WithMany("Lines")
                        .HasForeignKey("ReservationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_reservation_lines_reservations_reservation_id");

                    b.Navigation("Item");

                    b.Navigation("Reservation");
                });

            modelBuilder.Entity("InventoryService.Api.Entities.Stock", b =>
                {
                    b.HasOne("InventoryService.Api.Entities.Item", "Item")
                        .WithOne("Stock")
                        .HasForeignKey("InventoryService.Api.Entities.Stock", "ItemId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_stock_items_item_id");

                    b.Navigation("Item");
                });

            modelBuilder.Entity("InventoryService.Api.Entities.StockMovement", b =>
                {
                    b.HasOne("InventoryService.Api.Entities.Item", "Item")
                        .WithMany()
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_stock_movements_items_item_id");

                    b.HasOne("InventoryService.Api.Entities.Reservation", "Reservation")
                        .WithMany()
                        .HasForeignKey("ReservationId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_stock_movements_reservations_reservation_id");

                    b.HasOne("InventoryService.Api.Entities.Uom", "Uom")
                        .WithMany()
                        .HasForeignKey("UomId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_stock_movements_uoms_uom_id");

                    b.Navigation("Item");

                    b.Navigation("Reservation");

                    b.Navigation("Uom");
                });

            modelBuilder.Entity("InventoryService.Api.Entities.Item", b =>
                {
                    b.Navigation("Stock");

                    b.Navigation("Units");
                });

            modelBuilder.Entity("InventoryService.Api.Entities.Reservation", b =>
                {
                    b.Navigation("Lines");
                });

            modelBuilder.Entity("InventoryService.Api.Entities.Uom", b =>
                {
                    b.Navigation("ItemUnits");
                });
#pragma warning restore 612, 618
        }
    }
}
