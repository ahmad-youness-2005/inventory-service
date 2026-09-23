namespace InventoryService.Api.Entities;

public class Stock : IHasTimestamps
{
    public Guid ItemId { get; set; }

    public int Quantity { get; set; }

    public int Reserved { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Item Item { get; set; } = null!;
}
