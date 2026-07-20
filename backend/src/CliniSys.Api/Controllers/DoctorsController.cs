using CliniSys.Api.Requests.Doctors;
using CliniSys.Application.Commands.Doctors.UpdateDoctor;
using CliniSys.Application.Queries.Doctors.GetDoctors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for viewing and updating doctor profiles.</summary>
[ApiController, Route("api/doctors"), Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public DoctorsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns a paginated list of active doctors.</summary>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated doctor list.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetDoctorsQuery(page, pageSize), ct));

    /// <summary>Returns a single doctor by ID.</summary>
    /// <param name="id">Doctor identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The doctor or 404.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDoctorsQuery(1, 1000), ct);
        var doctor = result.Items.FirstOrDefault(d => d.Id == id);
        return doctor is null ? NotFound() : Ok(doctor);
    }

    /// <summary>Updates a doctor's specialty. Admin only.</summary>
    /// <param name="id">Doctor identifier.</param>
    /// <param name="request">Updated specialty.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdateDoctorRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateDoctorCommand(id, request.Specialty), ct);
        return NoContent();
    }
}
