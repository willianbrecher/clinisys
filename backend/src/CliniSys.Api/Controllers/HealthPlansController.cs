using CliniSys.Api.Requests.HealthPlans;
using CliniSys.Application.Commands.HealthPlans.CreateHealthPlan;
using CliniSys.Application.Commands.HealthPlans.DeactivateHealthPlan;
using CliniSys.Application.Commands.HealthPlans.UpdateHealthPlan;
using CliniSys.Application.Queries.HealthPlans.GetHealthPlanById;
using CliniSys.Application.Queries.HealthPlans.GetHealthPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for managing health plan records.</summary>
[ApiController, Route("api/health-plans"), Authorize(Roles = "Admin,Staff")]
public class HealthPlansController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public HealthPlansController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns a paginated list of active health plans, optionally filtered by name.</summary>
    /// <param name="search">Optional name filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged health plan list.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetHealthPlansQuery(search, page, pageSize), ct));

    /// <summary>Returns a single health plan by ID.</summary>
    /// <param name="id">Health plan identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The health plan or 404.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var plan = await _mediator.Send(new GetHealthPlanByIdQuery(id), ct);
        return plan is null ? NotFound() : Ok(plan);
    }

    /// <summary>Creates a new health plan.</summary>
    /// <param name="request">Health plan creation data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with the new health plan ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHealthPlanRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateHealthPlanCommand(request.Name, request.Notes), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Updates a health plan's details.</summary>
    /// <param name="id">Health plan identifier.</param>
    /// <param name="request">Updated data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdateHealthPlanRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateHealthPlanCommand(id, request.Name, request.Notes), ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a health plan (sets IsActive = false).</summary>
    /// <param name="id">Health plan identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateHealthPlanCommand(id), ct);
        return NoContent();
    }
}
