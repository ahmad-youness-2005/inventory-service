namespace InventoryService.Api.Entities;

public class Item : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Sku { get; set; }
    public required string Name { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ItemUom> Units { get; } = new List<ItemUom>();

    public Stock? Stock { get; set; }
}
