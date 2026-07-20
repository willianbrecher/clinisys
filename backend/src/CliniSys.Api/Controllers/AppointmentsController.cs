using System.Security.Claims;
using CliniSys.Api.Requests.Appointments;
using CliniSys.Application.Commands.Appointments.CreateAppointment;
using CliniSys.Application.Commands.Appointments.RescheduleAppointment;
using CliniSys.Application.Commands.Appointments.UpdateAppointmentStatus;
using CliniSys.Application.Queries.Appointments.GetAppointments;
using CliniSys.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for managing appointments.</summary>
[ApiController, Route("api/appointments"), Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public AppointmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns appointments. Doctors are restricted to their own appointments.
    /// Pass startDate+endDate for calendar view (pagination ignored).
    /// </summary>
    /// <param name="doctorId">Optional doctor filter.</param>
    /// <param name="patientId">Optional patient filter.</param>
    /// <param name="date">Optional single-day filter.</param>
    /// <param name="startDate">Calendar range start.</param>
    /// <param name="endDate">Calendar range end.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated or date-range appointment list.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? doctorId, [FromQuery] Guid? patientId,
        [FromQuery] DateOnly? date, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] AppointmentStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var role = User.FindFirstValue("role");
        if (role == "Doctor")
        {
            var doctorIdClaim = User.FindFirstValue("doctorId");
            doctorId = doctorIdClaim is not null ? Guid.Parse(doctorIdClaim) : doctorId;
        }
        return Ok(await _mediator.Send(new GetAppointmentsQuery(
            doctorId, patientId, date, startDate, endDate, status, page, pageSize), ct));
    }

    /// <summary>Schedules a new appointment. Staff/Admin only.</summary>
    /// <param name="request">Appointment data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with the new appointment ID.</returns>
    [HttpPost, Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateAppointmentCommand(
            request.PatientId, request.DoctorId, request.StartsAt,
            request.DurationMinutes, request.Notes), ct);
        return CreatedAtAction(nameof(GetAll), new { }, new { id });
    }

    /// <summary>Reschedules an existing appointment. Staff/Admin only.</summary>
    /// <param name="id">Appointment identifier.</param>
    /// <param name="request">New time and duration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut("{id:guid}"), Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Reschedule(
        [FromRoute] Guid id, [FromBody] RescheduleAppointmentRequest request, CancellationToken ct)
    {
        await _mediator.Send(new RescheduleAppointmentCommand(id, request.StartsAt, request.DurationMinutes), ct);
        return NoContent();
    }

    /// <summary>Updates appointment status.</summary>
    /// <param name="id">Appointment identifier.</param>
    /// <param name="request">Target status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid id, [FromBody] UpdateAppointmentStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateAppointmentStatusCommand(id, request.Status), ct);
        return NoContent();
    }
}
