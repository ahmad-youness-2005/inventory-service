using InventoryService.Api.Dtos;
using InventoryService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("uoms")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public sealed class UomsController(UomService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<UomResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UomResponse>> Create(CreateUomRequest request, CancellationToken ct)
    {
        var uom = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = uom.Id }, uom);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UomResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<UomResponse>> GetAll(CancellationToken ct) =>
        await service.GetAllAsync(ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UomResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<UomResponse> GetById(Guid id, CancellationToken ct) =>
        await service.GetByIdAsync(id, ct);
}
