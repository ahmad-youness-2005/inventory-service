using InventoryService.Api.Data;
using InventoryService.Api.Dtos;
using InventoryService.Api.Entities;
using InventoryService.Api.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace InventoryService.Api.Services;

public sealed class ReservationService(
    InventoryDbContext db,
    StockService stockService,
    IOptions<ReservationOptions> options)
{
    private const string OrderReferenceIndexName = "ix_reservations_order_reference";

    public async Task<(ReservationResponse Reservation, bool Created)> CreateAsync(
        CreateReservationRequest request, CancellationToken ct)
    {
        var orderReference = request.OrderReference.Trim();
        var lines = await ToPiecesAsync(request.Lines, ct);

        var active = await FindActiveAsync(orderReference, ct);
        if (active is not null)
        {
            if (active.Status == ReservationStatus.Pending && active.ExpiresAt <= await db.GetDatabaseNowAsync(ct))
            {
                await TryFinishAsync(active.Id, ReservationStatus.Expired, ct);
            }
            else
            {
                return (EnsureSameLines(active, lines), false);
            }
        }

        try
        {
            return (await InsertAsync(orderReference, lines, ct), true);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: OrderReferenceIndexName
        })
        {
            db.ChangeTracker.Clear();
            var winner = await FindActiveAsync(orderReference, ct)
                ?? throw new ConflictException($"Order '{orderReference}' changed at the same moment. Please retry.");
            return (EnsureSameLines(winner, lines), false);
        }
    }

    public async Task<ReservationResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        await db.Reservations
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(ToResponseExpression)
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException($"Reservation '{id}' was not found.");

    public async Task<IReadOnlyList<ReservationResponse>> GetByOrderReferenceAsync(string orderReference, CancellationToken ct)
    {
        var reference = orderReference.Trim();

        return await db.Reservations
            .AsNoTracking()
            .Where(r => r.OrderReference == reference)
            .OrderByDescending(r => r.CreatedAt)
            .Select(ToResponseExpression)
            .ToListAsync(ct);
    }

    public async Task<ReservationResponse> ConfirmAsync(Guid id, CancellationToken ct)
    {
        if (!await TryFinishAsync(id, ReservationStatus.Confirmed, ct))
            await ThrowInvalidStateAsync(id, "confirmed", ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<ReservationResponse> ReleaseAsync(Guid id, CancellationToken ct)
    {
        if (!await TryFinishAsync(id, ReservationStatus.Released, ct))
            await ThrowInvalidStateAsync(id, "released", ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<int> ExpireOverdueAsync(CancellationToken ct)
    {
        var now = await db.GetDatabaseNowAsync(ct);

        var overdueIds = await db.Reservations
            .Where(r => r.Status == ReservationStatus.Pending && r.ExpiresAt <= now)
            .OrderBy(r => r.ExpiresAt)
            .Select(r => r.Id)
            .Take(100)
            .ToListAsync(ct);

        var expired = 0;
        foreach (var id in overdueIds)
        {
            if (await TryFinishAsync(id, ReservationStatus.Expired, ct))
                expired++;
        }

        return expired;
    }

    private async Task<ReservationResponse> InsertAsync(
        string orderReference, IReadOnlyList<RequestedLine> lines, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = await db.GetDatabaseNowAsync(ct);

        foreach (var line in lines)
        {
            var updatedRows = await db.Stocks
                .Where(s => s.ItemId == line.ItemId && s.Quantity - s.Reserved >= line.Pieces)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Reserved, x => x.Reserved + line.Pieces)
                    .SetProperty(x => x.UpdatedAt, now), ct);

            if (updatedRows == 0)
                throw new ConflictException(await stockService.BuildInsufficientStockMessageAsync(line.ItemId, line.Pieces, ct));
        }

        var reservation = new Reservation
        {
            OrderReference = orderReference,
            ExpiresAt = now.AddMinutes(options.Value.ExpiryMinutes)
        };
        foreach (var line in lines)
            reservation.Lines.Add(new ReservationLine { ItemId = line.ItemId, Quantity = line.Pieces });

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(ct);

        await stockService.RecordMovementsAsync(
            StockMovementType.Reserved,
            lines.Select(l => new StockChange(l.ItemId, 0, l.Pieces)).ToList(),
            now, ct, reservationId: reservation.Id);

        await transaction.CommitAsync(ct);

        return ToResponse(reservation);
    }

    private async Task<bool> TryFinishAsync(Guid id, ReservationStatus newStatus, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = await db.GetDatabaseNowAsync(ct);

        var query = db.Reservations.Where(r => r.Id == id && r.Status == ReservationStatus.Pending);

        query = newStatus switch
        {
            ReservationStatus.Confirmed => query.Where(r => r.ExpiresAt > now),
            ReservationStatus.Expired => query.Where(r => r.ExpiresAt <= now),
            _ => query
        };

        var updatedRows = await query.ExecuteUpdateAsync(r => r
            .SetProperty(x => x.Status, newStatus)
            .SetProperty(x => x.UpdatedAt, now), ct);

        if (updatedRows == 0)
            return false;

        var lines = await db.ReservationLines
            .Where(l => l.ReservationId == id)
            .Select(l => new { l.ItemId, l.Quantity })
            .ToListAsync(ct);

        foreach (var line in lines.OrderBy(l => l.ItemId))
        {
            var stock = db.Stocks.Where(s => s.ItemId == line.ItemId);

            if (newStatus == ReservationStatus.Confirmed)
            {
                await stock.ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Quantity, x => x.Quantity - line.Quantity)
                    .SetProperty(x => x.Reserved, x => x.Reserved - line.Quantity)
                    .SetProperty(x => x.UpdatedAt, now), ct);
            }
            else
            {
                await stock.ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Reserved, x => x.Reserved - line.Quantity)
                    .SetProperty(x => x.UpdatedAt, now), ct);
            }
        }

        var movementType = newStatus switch
        {
            ReservationStatus.Confirmed => StockMovementType.Confirmed,
            ReservationStatus.Released => StockMovementType.Released,
            _ => StockMovementType.Expired
        };

        await stockService.RecordMovementsAsync(
            movementType,
            lines
                .Select(l => newStatus == ReservationStatus.Confirmed
                    ? new StockChange(l.ItemId, -l.Quantity, -l.Quantity)
                    : new StockChange(l.ItemId, 0, -l.Quantity))
                .ToList(),
            now, ct, reservationId: id);

        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
        return true;
    }

    private async Task<IReadOnlyList<RequestedLine>> ToPiecesAsync(
        IReadOnlyList<ReservationLineRequest> requestLines, CancellationToken ct)
    {
        var totals = new Dictionary<Guid, long>();

        foreach (var line in requestLines)
        {
            var itemId = line.ItemId!.Value;
            var pieces = await stockService.ConvertToPiecesAsync(itemId, line.UomId, line.Quantity, ct);
            totals[itemId] = totals.GetValueOrDefault(itemId) + pieces;
        }

        if (totals.Values.Any(pieces => pieces > int.MaxValue))
            throw new BadRequestException("Quantity is too large.");

        return totals
            .OrderBy(t => t.Key)
            .Select(t => new RequestedLine(t.Key, (int)t.Value))
            .ToList();
    }

    private Task<ReservationResponse?> FindActiveAsync(string orderReference, CancellationToken ct) =>
        db.Reservations
            .AsNoTracking()
            .Where(r => r.OrderReference == orderReference
                        && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed))
            .Select(ToResponseExpression)
            .FirstOrDefaultAsync(ct);

    private static ReservationResponse EnsureSameLines(ReservationResponse existing, IReadOnlyList<RequestedLine> requested)
    {
        var sameLines = existing.Lines
            .OrderBy(l => l.ItemId)
            .Select(l => new RequestedLine(l.ItemId, l.Quantity))
            .SequenceEqual(requested);

        if (sameLines)
            return existing;

        throw new ConflictException(
            $"Order '{existing.OrderReference}' already has a {existing.Status} reservation ({existing.Id}) with different items. " +
            "Release it first to reserve different items.");
    }

    private async Task ThrowInvalidStateAsync(Guid id, string action, CancellationToken ct)
    {
        var current = await GetByIdAsync(id, ct);

        if (current.Status == ReservationStatus.Pending)
            throw new ConflictException($"Reservation can't be {action}: it expired at {current.ExpiresAt:O}.");

        throw new ConflictException($"Reservation can't be {action}: it is already {current.Status}.");
    }

    private static readonly System.Linq.Expressions.Expression<Func<Reservation, ReservationResponse>> ToResponseExpression =
        r => new ReservationResponse(
            r.Id,
            r.OrderReference,
            r.Status,
            r.Lines
                .OrderBy(l => l.ItemId)
                .Select(l => new ReservationLineResponse(l.ItemId, l.Quantity))
                .ToList(),
            r.ExpiresAt,
            r.CreatedAt,
            r.UpdatedAt);

    private static ReservationResponse ToResponse(Reservation r) =>
        new(r.Id,
            r.OrderReference,
            r.Status,
            r.Lines
                .OrderBy(l => l.ItemId)
                .Select(l => new ReservationLineResponse(l.ItemId, l.Quantity))
                .ToList(),
            r.ExpiresAt,
            r.CreatedAt,
            r.UpdatedAt);

    private sealed record RequestedLine(Guid ItemId, int Pieces);
}
