using System.Security.Claims;
using CliniSys.Api.Requests.Auth;
using CliniSys.Application.Commands.Auth.ChangePassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Auth endpoints beyond the OpenIddict token endpoint.</summary>
[ApiController, Route("api/auth"), Authorize]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>Allows the authenticated user to change their own password.</summary>
    /// <param name="request">Current and new passwords.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);
        await _mediator.Send(new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword), ct);
        return NoContent();
    }
}
