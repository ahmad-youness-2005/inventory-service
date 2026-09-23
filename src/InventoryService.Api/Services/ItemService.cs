using InventoryService.Api.Data;
using InventoryService.Api.Dtos;
using InventoryService.Api.Entities;
using InventoryService.Api.Errors;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Api.Services;

public sealed class ItemService(InventoryDbContext db)
{
    public async Task<ItemResponse> CreateAsync(CreateItemRequest request, CancellationToken ct)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();

        if (await db.Items.AnyAsync(i => i.Sku == sku, ct))
            throw new ConflictException($"An item with SKU '{sku}' already exists.");

        var item = new Item
        {
            Sku = sku,
            Name = request.Name.Trim(),
            Stock = new Stock { Quantity = 0 }
        };

        db.Items.Add(item);
        await db.SaveChangesAsync(ct);

        return new ItemResponse(item.Id, item.Sku, item.Name, item.CreatedAt, item.UpdatedAt);
    }

    public async Task<PagedResponse<ItemResponse>> GetPageAsync(int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await db.Items.CountAsync(ct);

        var items = await db.Items
            .AsNoTracking()
            .OrderBy(i => i.Sku)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new ItemResponse(i.Id, i.Sku, i.Name, i.CreatedAt, i.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResponse<ItemResponse>(items, page, pageSize, totalCount);
    }

    public async Task<ItemResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        await db.Items
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new ItemResponse(i.Id, i.Sku, i.Name, i.CreatedAt, i.UpdatedAt))
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException($"Item '{id}' was not found.");
}
