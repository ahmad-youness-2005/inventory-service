using InventoryService.Api.Data;
using InventoryService.Api.Dtos;
using InventoryService.Api.Entities;
using InventoryService.Api.Errors;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Api.Services;

public sealed class UomService(InventoryDbContext db)
{
    public async Task<UomResponse> CreateAsync(CreateUomRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim().ToUpperInvariant();

        if (await db.Uoms.AnyAsync(u => u.Name == name, ct))
            throw new ConflictException($"A unit named '{name}' already exists.");

        var uom = new Uom
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        db.Uoms.Add(uom);
        await db.SaveChangesAsync(ct);

        return new UomResponse(uom.Id, uom.Name, uom.Description, uom.CreatedAt, uom.UpdatedAt);
    }

    public async Task<IReadOnlyList<UomResponse>> GetAllAsync(CancellationToken ct) =>
        await db.Uoms
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new UomResponse(u.Id, u.Name, u.Description, u.CreatedAt, u.UpdatedAt))
            .ToListAsync(ct);

    public async Task<UomResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        await db.Uoms
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UomResponse(u.Id, u.Name, u.Description, u.CreatedAt, u.UpdatedAt))
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException($"Unit '{id}' was not found.");
}
