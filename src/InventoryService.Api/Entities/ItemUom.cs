namespace InventoryService.Api.Entities;

public class ItemUom : IHasTimestamps
{
    public Guid ItemId { get; set; }
    public Guid UomId { get; set; }

    public int QuantityPerUnit { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Item Item { get; set; } = null!;
    public Uom Uom { get; set; } = null!;
}
