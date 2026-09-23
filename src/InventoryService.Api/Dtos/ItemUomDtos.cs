using System.ComponentModel.DataAnnotations;

namespace InventoryService.Api.Dtos;

public sealed record AddItemUomRequest
{
    [Required]
    public Guid? UomId { get; init; }

    [Range(1, int.MaxValue)]
    public int QuantityPerUnit { get; init; }
}

public sealed record ItemUomResponse(
    Guid ItemId,
    Guid UomId,
    string UomName,
    int QuantityPerUnit,
    DateTime CreatedAt,
    DateTime UpdatedAt);
