namespace InventoryService.Api.Entities;

public class ReservationLine
{
    public Guid ReservationId { get; set; }
    public Guid ItemId { get; set; }

    public int Quantity { get; set; }

    public Reservation Reservation { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
