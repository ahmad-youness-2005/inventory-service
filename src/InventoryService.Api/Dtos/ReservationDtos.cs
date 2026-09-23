using System.ComponentModel.DataAnnotations;
using InventoryService.Api.Entities;

namespace InventoryService.Api.Dtos;

public sealed record CreateReservationRequest
{
    [Required, StringLength(100)]
    public string OrderReference { get; init; } = "";

    [Required, MinLength(1), MaxLength(100)]
    public IReadOnlyList<ReservationLineRequest> Lines { get; init; } = [];
}

public sealed record ReservationLineRequest
{
    [Required]
    public Guid? ItemId { get; init; }

    public Guid? UomId { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}

public sealed record ReservationResponse(
    Guid Id,
    string OrderReference,
    ReservationStatus Status,
    IReadOnlyList<ReservationLineResponse> Lines,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ReservationLineResponse(Guid ItemId, int Quantity);
