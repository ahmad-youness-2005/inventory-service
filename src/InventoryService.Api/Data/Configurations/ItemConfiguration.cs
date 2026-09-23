using InventoryService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Api.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Sku).HasMaxLength(20).IsRequired();
        builder.HasIndex(i => i.Sku).IsUnique();

        builder.Property(i => i.Name).HasMaxLength(150).IsRequired();

        builder.HasOne(i => i.Stock)
            .WithOne(s => s.Item)
            .HasForeignKey<Stock>(s => s.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
