using System.ComponentModel.DataAnnotations;
using InventoryService.Api.Entities;

namespace InventoryService.Api.Dtos;

public sealed record StockChangeRequest
{
    public Guid? UomId { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    [StringLength(200)]
    public string? Reason { get; init; }
}

public sealed record StockResponse(
    Guid ItemId,
    string Sku,
    string Name,
    int QuantityInPieces,
    int ReservedInPieces,
    int AvailableInPieces,
    IReadOnlyList<StockInUnit> Units,
    DateTime UpdatedAt);

public sealed record StockInUnit(
    Guid UomId,
    string UomName,
    int QuantityPerUnit,
    int FullUnits,
    int RemainingPieces);

public sealed record StockMovementResponse(
    long Id,
    StockMovementType Type,
    int QuantityChange,
    int ReservedChange,
    int QuantityAfter,
    int ReservedAfter,
    Guid? UomId,
    string? UomName,
    int? UnitQuantity,
    string? Reason,
    Guid? ReservationId,
    DateTime CreatedAt);
