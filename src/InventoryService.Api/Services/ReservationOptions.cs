using System.ComponentModel.DataAnnotations;

namespace InventoryService.Api.Services;

public sealed class ReservationOptions
{
    public const string SectionName = "Reservations";

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 15;
}
