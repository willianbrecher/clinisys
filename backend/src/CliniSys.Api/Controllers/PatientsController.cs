using CliniSys.Api.Requests.Patients;
using CliniSys.Application.Commands.Patients.CreatePatient;
using CliniSys.Application.Commands.Patients.DeactivatePatient;
using CliniSys.Application.Commands.Patients.UpdatePatient;
using CliniSys.Application.Queries.Patients.GetPatientById;
using CliniSys.Application.Queries.Patients.GetPatients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for managing patient records.</summary>
[ApiController, Route("api/patients"), Authorize(Roles = "Admin,Staff")]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public PatientsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns a paginated list of active patients, optionally filtered by name.</summary>
    /// <param name="search">Optional name filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged patient list.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetPatientsQuery(search, page, pageSize), ct));

    /// <summary>Returns a single patient by ID.</summary>
    /// <param name="id">Patient identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The patient or 404.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var patient = await _mediator.Send(new GetPatientByIdQuery(id), ct);
        return patient is null ? NotFound() : Ok(patient);
    }

    /// <summary>Creates a new patient.</summary>
    /// <param name="request">Patient creation data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with the new patient ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreatePatientCommand(
            request.FullName, request.DateOfBirth, request.Phone, request.Email, request.Notes,
            request.HealthPlanId, request.HealthPlanNumber), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Updates a patient's details.</summary>
    /// <param name="id">Patient identifier.</param>
    /// <param name="request">Updated data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdatePatientRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdatePatientCommand(
            id, request.FullName, request.DateOfBirth, request.Phone, request.Email, request.Notes,
            request.HealthPlanId, request.HealthPlanNumber), ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a patient (sets IsActive = false).</summary>
    /// <param name="id">Patient identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivatePatientCommand(id), ct);
        return NoContent();
    }
}
