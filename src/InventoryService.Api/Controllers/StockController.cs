using System.ComponentModel.DataAnnotations;
using InventoryService.Api.Dtos;
using InventoryService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("items/{itemId:guid}/stock")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public sealed class StockController(StockService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<StockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<StockResponse> Get(Guid itemId, CancellationToken ct) =>
        await service.GetAsync(itemId, ct);

    [HttpPost("in")]
    [ProducesResponseType<StockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<StockResponse> StockIn(Guid itemId, StockChangeRequest request, CancellationToken ct) =>
        await service.StockInAsync(itemId, request, ct);

    [HttpPost("out")]
    [ProducesResponseType<StockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<StockResponse> StockOut(Guid itemId, StockChangeRequest request, CancellationToken ct) =>
        await service.StockOutAsync(itemId, request, ct);

    [HttpGet("history")]
    [ProducesResponseType<PagedResponse<StockMovementResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<PagedResponse<StockMovementResponse>> GetHistory(
        Guid itemId,
        [FromQuery, Range(1, 1_000_000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default) =>
        await service.GetHistoryAsync(itemId, page, pageSize, ct);
}
