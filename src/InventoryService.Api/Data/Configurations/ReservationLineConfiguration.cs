using InventoryService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Api.Data.Configurations;

public class ReservationLineConfiguration : IEntityTypeConfiguration<ReservationLine>
{
    public void Configure(EntityTypeBuilder<ReservationLine> builder)
    {
        builder.ToTable("reservation_lines", t =>
            t.HasCheckConstraint("ck_reservation_lines_quantity_positive", "quantity > 0"));

        builder.HasKey(l => new { l.ReservationId, l.ItemId });

        builder.HasOne(l => l.Reservation)
            .WithMany(r => r.Lines)
            .HasForeignKey(l => l.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
