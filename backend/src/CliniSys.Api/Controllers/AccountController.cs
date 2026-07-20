using System.Security.Claims;
using CliniSys.Api.Requests.Account;
using CliniSys.Application.Commands.Account.UpdatePreferences;
using CliniSys.Application.Commands.Account.UpdateProfilePicture;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Self-service account endpoints available to all authenticated roles.</summary>
[ApiController, Route("api/account"), Authorize]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public AccountController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    /// <summary>Sets or removes the authenticated user's profile picture.</summary>
    /// <param name="request">Base64 data URI or null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("profile-picture")]
    public async Task<IActionResult> UpdateProfilePicture(
        [FromBody] UpdateProfilePictureRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateProfilePictureCommand(CurrentUserId, request.ProfilePictureBase64), ct);
        return NoContent();
    }

    /// <summary>Updates the authenticated user's theme and language preferences.</summary>
    /// <param name="request">New preferences.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("preferences")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdatePreferencesCommand(CurrentUserId, request.Theme, request.Language), ct);
        return NoContent();
    }
}
