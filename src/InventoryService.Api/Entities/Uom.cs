namespace InventoryService.Api.Entities;

public class Uom : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Name { get; set; }
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ItemUom> ItemUnits { get; } = new List<ItemUom>();
}
