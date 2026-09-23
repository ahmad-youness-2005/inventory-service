using System.ComponentModel.DataAnnotations;

namespace InventoryService.Api.Dtos;

public sealed record CreateItemRequest
{
    [Required, StringLength(20)]
    public string Sku { get; init; } = "";

    [Required, StringLength(150)]
    public string Name { get; init; } = "";
}

public sealed record ItemResponse(
    Guid Id,
    string Sku,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
