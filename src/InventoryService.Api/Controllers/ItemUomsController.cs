using InventoryService.Api.Dtos;
using InventoryService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("items/{itemId:guid}/uoms")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public sealed class ItemUomsController(ItemUomService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ItemUomResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ItemUomResponse>> Add(Guid itemId, AddItemUomRequest request, CancellationToken ct)
    {
        var link = await service.AddAsync(itemId, request, ct);
        return CreatedAtAction(nameof(GetAll), new { itemId }, link);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ItemUomResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IReadOnlyList<ItemUomResponse>> GetAll(Guid itemId, CancellationToken ct) =>
        await service.GetForItemAsync(itemId, ct);
}
