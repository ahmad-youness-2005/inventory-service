using InventoryService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Api.Data.Configurations;

public class ItemUomConfiguration : IEntityTypeConfiguration<ItemUom>
{
    public void Configure(EntityTypeBuilder<ItemUom> builder)
    {
        builder.ToTable("item_uoms", t =>
            t.HasCheckConstraint("ck_item_uoms_quantity_per_unit_positive", "quantity_per_unit > 0"));

        builder.HasKey(iu => new { iu.ItemId, iu.UomId });

        builder.HasOne(iu => iu.Item)
            .WithMany(i => i.Units)
            .HasForeignKey(iu => iu.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(iu => iu.Uom)
            .WithMany(u => u.ItemUnits)
            .HasForeignKey(iu => iu.UomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
