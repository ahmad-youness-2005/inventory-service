using System.ComponentModel.DataAnnotations;

namespace InventoryService.Api.Dtos;

public sealed record CreateUomRequest
{
    [Required, StringLength(50)]
    public string Name { get; init; } = "";

    [StringLength(150)]
    public string? Description { get; init; }
}

public sealed record UomResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);
