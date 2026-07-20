using CliniSys.Domain.Enums;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>
/// Abstraction over ASP.NET Core Identity operations used by Application handlers.
/// Keeps handlers free from <c>UserManager</c> and Identity SDK types.
/// </summary>
public interface IIdentityService
{
    /// <summary>Creates a new user account with the given password and role.</summary>
    /// <param name="email">Email address (also used as username).</param>
    /// <param name="fullName">Display name.</param>
    /// <param name="password">Plain-text password (hashed by Identity).</param>
    /// <param name="role">The user's role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new user's <see cref="Guid"/> identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Identity reports errors.</exception>
    Task<Guid> CreateUserAsync(string email, string fullName, string password, Role role, CancellationToken ct = default);

    /// <summary>Resets a user's password to a new value (admin action — no current password required).</summary>
    /// <param name="userId">Target user identifier.</param>
    /// <param name="newPassword">The new plain-text password.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);

    /// <summary>Changes the calling user's own password (requires current password).</summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="currentPassword">Current plain-text password for verification.</param>
    /// <param name="newPassword">New plain-text password.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>Locks a user out indefinitely (soft deactivation).</summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeactivateUserAsync(Guid userId, CancellationToken ct = default);
}
