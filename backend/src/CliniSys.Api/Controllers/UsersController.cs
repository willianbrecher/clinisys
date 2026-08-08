using CliniSys.Api.Requests.Users;
using CliniSys.Application.Commands.Users.CreateUser;
using CliniSys.Application.Commands.Users.DeactivateUser;
using CliniSys.Application.Commands.Users.ReactivateUser;
using CliniSys.Application.Commands.Users.ResetPassword;
using CliniSys.Application.Queries.Users.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for Admin user management.</summary>
[ApiController, Route("api/users"), Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public UsersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns a paginated list of all users.</summary>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated user list.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetUsersQuery(page, pageSize), ct));

    /// <summary>Creates a new user account (and Doctor profile when role is Doctor).</summary>
    /// <param name="request">User creation data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with the new user ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateUserCommand(
            request.Email, request.FullName, request.Password, request.Role, request.Specialty), ct);
        return CreatedAtAction(null, new { id }, new { id });
    }

    /// <summary>Locks a user account indefinitely.</summary>
    /// <param name="id">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateUserCommand(id), ct);
        return NoContent();
    }

    /// <summary>Clears a user account's lockout.</summary>
    /// <param name="id">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate([FromRoute] Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ReactivateUserCommand(id), ct);
        return NoContent();
    }

    /// <summary>Resets a user's password (Admin action; no current password required).</summary>
    /// <param name="id">Target user identifier.</param>
    /// <param name="request">New password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromRoute] Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _mediator.Send(new ResetPasswordCommand(id, request.NewPassword), ct);
        return NoContent();
    }
}
