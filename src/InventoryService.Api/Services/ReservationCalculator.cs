using InventoryService.Api.Dtos;
using InventoryService.Api.Entities;
using InventoryService.Api.Errors;

namespace InventoryService.Api.Services;

public sealed record RequestedLine(Guid ItemId, int Pieces);

public static class ReservationCalculator
{
    public static IReadOnlyList<RequestedLine> Merge(IEnumerable<RequestedLine> lines)
    {
        var totals = new Dictionary<Guid, long>();

        foreach (var line in lines)
            totals[line.ItemId] = totals.GetValueOrDefault(line.ItemId) + line.Pieces;

        if (totals.Values.Any(pieces => pieces > int.MaxValue))
            throw new BadRequestException("Quantity is too large.");

        return totals
            .OrderBy(t => t.Key)
            .Select(t => new RequestedLine(t.Key, (int)t.Value))
            .ToList();
    }

    public static bool HasSameLines(IEnumerable<ReservationLineResponse> existing, IReadOnlyList<RequestedLine> requested) =>
        existing
            .OrderBy(l => l.ItemId)
            .Select(l => new RequestedLine(l.ItemId, l.Quantity))
            .SequenceEqual(requested);

    public static StockChange StockChangeFor(ReservationStatus status, Guid itemId, int pieces) => status switch
    {
        ReservationStatus.Pending => new StockChange(itemId, 0, pieces),
        ReservationStatus.Confirmed => new StockChange(itemId, -pieces, -pieces),
        ReservationStatus.Released or ReservationStatus.Expired => new StockChange(itemId, 0, -pieces),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static StockMovementType MovementTypeFor(ReservationStatus status) => status switch
    {
        ReservationStatus.Pending => StockMovementType.Reserved,
        ReservationStatus.Confirmed => StockMovementType.Confirmed,
        ReservationStatus.Released => StockMovementType.Released,
        ReservationStatus.Expired => StockMovementType.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
