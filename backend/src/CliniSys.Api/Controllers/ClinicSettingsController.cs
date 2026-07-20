using CliniSys.Api.Requests.ClinicSettings;
using CliniSys.Application.Commands.ClinicSettings.UpdateClinicSettings;
using CliniSys.Application.Queries.ClinicSettings.GetClinicSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for reading and updating clinic-wide settings.</summary>
[ApiController, Route("api/clinic-settings"), Authorize]
public class ClinicSettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public ClinicSettingsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns current clinic settings. All authenticated roles.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Clinic settings.</returns>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetClinicSettingsQuery(), ct));

    /// <summary>Updates clinic settings. Admin only.</summary>
    /// <param name="request">New settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] UpdateClinicSettingsRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateClinicSettingsCommand(
            request.OpenTime, request.CloseTime, request.OpenDays, request.LogoBase64), ct);
        return NoContent();
    }
}
