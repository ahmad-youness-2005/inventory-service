using InventoryService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Api.Data.Configurations;

public class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("stock", t =>
        {
            t.HasCheckConstraint("ck_stock_quantity_not_negative", "quantity >= 0");
            t.HasCheckConstraint("ck_stock_reserved_not_negative", "reserved >= 0");
            t.HasCheckConstraint("ck_stock_reserved_not_above_quantity", "reserved <= quantity");
        });

        builder.HasKey(s => s.ItemId);
        builder.Property(s => s.ItemId).ValueGeneratedNever();

        builder.Property(s => s.Quantity).HasDefaultValue(0);
        builder.Property(s => s.Reserved).HasDefaultValue(0);
    }
}
