namespace InventoryService.Api.Entities;

public class StockMovement
{
    public long Id { get; set; }

    public Guid ItemId { get; set; }

    public StockMovementType Type { get; set; }

    public int QuantityChange { get; set; }
    public int ReservedChange { get; set; }

    public int QuantityAfter { get; set; }
    public int ReservedAfter { get; set; }

    public Guid? UomId { get; set; }
    public int? UnitQuantity { get; set; }

    public string? Reason { get; set; }

    public Guid? ReservationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Item Item { get; set; } = null!;
    public Uom? Uom { get; set; }
    public Reservation? Reservation { get; set; }
}

public enum StockMovementType
{
    OpeningBalance,
    StockIn,
    StockOut,
    Reserved,
    Confirmed,
    Released,
    Expired
}
