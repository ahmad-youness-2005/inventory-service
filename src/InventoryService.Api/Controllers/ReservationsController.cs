using System.ComponentModel.DataAnnotations;
using InventoryService.Api.Dtos;
using InventoryService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("reservations")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public sealed class ReservationsController(ReservationService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Create(CreateReservationRequest request, CancellationToken ct)
    {
        var (reservation, created) = await service.CreateAsync(request, ct);

        return created
            ? CreatedAtAction(nameof(GetById), new { id = reservation.Id }, reservation)
            : Ok(reservation);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ReservationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<ReservationResponse>> GetByOrder(
        [FromQuery, Required, StringLength(100)] string orderReference,
        CancellationToken ct) =>
        await service.GetByOrderReferenceAsync(orderReference, ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ReservationResponse> GetById(Guid id, CancellationToken ct) =>
        await service.GetByIdAsync(id, ct);

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ReservationResponse> Confirm(Guid id, CancellationToken ct) =>
        await service.ConfirmAsync(id, ct);

    [HttpPost("{id:guid}/release")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ReservationResponse> Release(Guid id, CancellationToken ct) =>
        await service.ReleaseAsync(id, ct);
}
