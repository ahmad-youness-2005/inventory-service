using InventoryService.Api.Data;
using InventoryService.Api.Dtos;
using InventoryService.Api.Entities;
using InventoryService.Api.Errors;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Api.Services;

public sealed class ItemUomService(InventoryDbContext db)
{
    public async Task<ItemUomResponse> AddAsync(Guid itemId, AddItemUomRequest request, CancellationToken ct)
    {
        var uomId = request.UomId!.Value;

        if (!await db.Items.AnyAsync(i => i.Id == itemId, ct))
            throw new NotFoundException($"Item '{itemId}' was not found.");

        var uomName = await db.Uoms
            .Where(u => u.Id == uomId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Unit '{uomId}' was not found.");

        if (await db.ItemUoms.AnyAsync(iu => iu.ItemId == itemId && iu.UomId == uomId, ct))
            throw new ConflictException($"Unit '{uomName}' is already linked to this item.");

        var link = new ItemUom
        {
            ItemId = itemId,
            UomId = uomId,
            QuantityPerUnit = request.QuantityPerUnit
        };

        db.ItemUoms.Add(link);
        await db.SaveChangesAsync(ct);

        return new ItemUomResponse(link.ItemId, link.UomId, uomName, link.QuantityPerUnit, link.CreatedAt, link.UpdatedAt);
    }

    public async Task<IReadOnlyList<ItemUomResponse>> GetForItemAsync(Guid itemId, CancellationToken ct)
    {
        if (!await db.Items.AnyAsync(i => i.Id == itemId, ct))
            throw new NotFoundException($"Item '{itemId}' was not found.");

        return await db.ItemUoms
            .AsNoTracking()
            .Where(iu => iu.ItemId == itemId)
            .OrderByDescending(iu => iu.QuantityPerUnit)
            .Select(iu => new ItemUomResponse(iu.ItemId, iu.UomId, iu.Uom.Name, iu.QuantityPerUnit, iu.CreatedAt, iu.UpdatedAt))
            .ToListAsync(ct);
    }
}
