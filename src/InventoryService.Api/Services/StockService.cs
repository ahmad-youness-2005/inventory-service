using InventoryService.Api.Data;
using InventoryService.Api.Dtos;
using InventoryService.Api.Entities;
using InventoryService.Api.Errors;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Api.Services;

public sealed class StockService(InventoryDbContext db)
{
    public async Task<StockResponse> StockInAsync(Guid itemId, StockChangeRequest request, CancellationToken ct)
    {
        var pieces = await ConvertToPiecesAsync(itemId, request.UomId, request.Quantity, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = await db.GetDatabaseNowAsync(ct);

        await db.Stocks
            .Where(s => s.ItemId == itemId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Quantity, x => x.Quantity + pieces)
                .SetProperty(x => x.UpdatedAt, now), ct);

        await RecordMovementsAsync(StockMovementType.StockIn, [new StockChange(itemId, pieces, 0)], now, ct, request: request);
        await transaction.CommitAsync(ct);

        return await GetAsync(itemId, ct);
    }

    public async Task<StockResponse> StockOutAsync(Guid itemId, StockChangeRequest request, CancellationToken ct)
    {
        var pieces = await ConvertToPiecesAsync(itemId, request.UomId, request.Quantity, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = await db.GetDatabaseNowAsync(ct);

        var updatedRows = await db.Stocks
            .Where(s => s.ItemId == itemId && s.Quantity - s.Reserved >= pieces)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Quantity, x => x.Quantity - pieces)
                .SetProperty(x => x.UpdatedAt, now), ct);

        if (updatedRows == 0)
            throw new ConflictException(await BuildInsufficientStockMessageAsync(itemId, pieces, ct));

        await RecordMovementsAsync(StockMovementType.StockOut, [new StockChange(itemId, -pieces, 0)], now, ct, request: request);
        await transaction.CommitAsync(ct);

        return await GetAsync(itemId, ct);
    }

    public async Task<StockResponse> GetAsync(Guid itemId, CancellationToken ct)
    {
        var row = await db.Items
            .AsNoTracking()
            .Where(i => i.Id == itemId)
            .Select(i => new
            {
                i.Id,
                i.Sku,
                i.Name,
                i.Stock!.Quantity,
                i.Stock.Reserved,
                i.Stock.UpdatedAt,
                Units = i.Units
                    .OrderByDescending(u => u.QuantityPerUnit)
                    .Select(u => new { u.UomId, UomName = u.Uom.Name, u.QuantityPerUnit })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Item '{itemId}' was not found.");

        var units = row.Units
            .Select(u => new StockInUnit(
                u.UomId,
                u.UomName,
                u.QuantityPerUnit,
                FullUnits: row.Quantity / u.QuantityPerUnit,
                RemainingPieces: row.Quantity % u.QuantityPerUnit))
            .ToList();

        return new StockResponse(
            row.Id, row.Sku, row.Name,
            QuantityInPieces: row.Quantity,
            ReservedInPieces: row.Reserved,
            AvailableInPieces: row.Quantity - row.Reserved,
            units,
            row.UpdatedAt);
    }

    public async Task<PagedResponse<StockMovementResponse>> GetHistoryAsync(
        Guid itemId, int page, int pageSize, CancellationToken ct)
    {
        if (!await db.Items.AnyAsync(i => i.Id == itemId, ct))
            throw new NotFoundException($"Item '{itemId}' was not found.");

        var movements = db.StockMovements.AsNoTracking().Where(m => m.ItemId == itemId);

        var totalCount = await movements.CountAsync(ct);

        var items = await movements
            .OrderByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new StockMovementResponse(
                m.Id,
                m.Type,
                m.QuantityChange,
                m.ReservedChange,
                m.QuantityAfter,
                m.ReservedAfter,
                m.UomId,
                m.Uom != null ? m.Uom.Name : null,
                m.UnitQuantity,
                m.Reason,
                m.ReservationId,
                m.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<StockMovementResponse>(items, page, pageSize, totalCount);
    }

    public async Task RecordMovementsAsync(
        StockMovementType type,
        IReadOnlyList<StockChange> changes,
        DateTime now,
        CancellationToken ct,
        Guid? reservationId = null,
        StockChangeRequest? request = null)
    {
        var itemIds = changes.Select(c => c.ItemId).ToList();

        var balances = await db.Stocks
            .AsNoTracking()
            .Where(s => itemIds.Contains(s.ItemId))
            .Select(s => new { s.ItemId, s.Quantity, s.Reserved })
            .ToDictionaryAsync(s => s.ItemId, ct);

        var reason = request?.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            reason = null;

        foreach (var change in changes)
        {
            var balance = balances[change.ItemId];

            db.StockMovements.Add(new StockMovement
            {
                ItemId = change.ItemId,
                Type = type,
                QuantityChange = change.QuantityChange,
                ReservedChange = change.ReservedChange,
                QuantityAfter = balance.Quantity,
                ReservedAfter = balance.Reserved,
                UomId = request?.UomId,
                UnitQuantity = request?.Quantity,
                Reason = reason,
                ReservationId = reservationId,
                CreatedAt = now
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<int> ConvertToPiecesAsync(Guid itemId, Guid? uomId, int quantity, CancellationToken ct)
    {
        if (!await db.Items.AnyAsync(i => i.Id == itemId, ct))
            throw new NotFoundException($"Item '{itemId}' was not found.");

        var quantityPerUnit = 1;

        if (uomId is { } id)
        {
            quantityPerUnit = await db.ItemUoms
                .Where(iu => iu.ItemId == itemId && iu.UomId == id)
                .Select(iu => (int?)iu.QuantityPerUnit)
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException(
                    $"Unit '{id}' is not linked to this item. Link it first with POST /items/{itemId}/uoms.");
        }

        var pieces = (long)quantity * quantityPerUnit;
        if (pieces > int.MaxValue)
            throw new BadRequestException("Quantity is too large.");

        return (int)pieces;
    }

    public async Task<string> BuildInsufficientStockMessageAsync(Guid itemId, int requestedPieces, CancellationToken ct)
    {
        var stock = await db.Stocks
            .Where(s => s.ItemId == itemId)
            .Select(s => new { s.Item.Sku, Available = s.Quantity - s.Reserved })
            .FirstAsync(ct);

        return $"Not enough stock of '{stock.Sku}'. Requested {requestedPieces} pieces, but only {stock.Available} are available.";
    }
}

public sealed record StockChange(Guid ItemId, int QuantityChange, int ReservedChange);
