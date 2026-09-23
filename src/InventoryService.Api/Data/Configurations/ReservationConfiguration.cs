using InventoryService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Api.Data.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(r => r.OrderReference).HasMaxLength(100).IsRequired();

        builder.HasIndex(r => new { r.Status, r.ExpiresAt });

        builder.HasIndex(r => r.OrderReference)
            .IsUnique()
            .HasFilter("status IN ('Pending', 'Confirmed')");
    }
}
