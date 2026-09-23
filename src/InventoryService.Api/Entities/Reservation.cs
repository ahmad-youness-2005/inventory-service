namespace InventoryService.Api.Entities;

public class Reservation : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string OrderReference { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ReservationLine> Lines { get; } = new List<ReservationLine>();
}

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Released,
    Expired
}
