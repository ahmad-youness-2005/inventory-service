using InventoryService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Api.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(m => m.Reason).HasMaxLength(200);

        builder.Property(m => m.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(m => new { m.ItemId, m.Id });

        builder.HasOne(m => m.Item)
            .WithMany()
            .HasForeignKey(m => m.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Uom)
            .WithMany()
            .HasForeignKey(m => m.UomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Reservation)
            .WithMany()
            .HasForeignKey(m => m.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
