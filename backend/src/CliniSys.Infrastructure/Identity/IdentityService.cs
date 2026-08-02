using CliniSys.Application.Common.Interfaces;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;

namespace CliniSys.Infrastructure.Identity;

internal class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager) =>
        _userManager = userManager;

    public async Task<Guid> CreateUserAsync(
        string email, string fullName, string password, Role role, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            Id       = Guid.NewGuid(),
            UserName = email,
            Email    = email,
            FullName = fullName,
            Role     = role
        };
        var result = await _userManager.CreateAsync(user, password);
        ThrowIfFailed(result);
        return user.Id;
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        ThrowIfFailed(result);
    }

    public async Task ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        ThrowIfFailed(result);
    }

    /// <summary>
    /// Surfaces Identity failures (e.g. password complexity, duplicate email) as a
    /// FluentValidation exception so the API returns a 400 with per-rule messages
    /// instead of an opaque 500.
    /// </summary>
    private static void ThrowIfFailed(IdentityResult result)
    {
        if (result.Succeeded) return;
        var failures = result.Errors.Select(e => new ValidationFailure(
            e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? "Password" : "Email",
            e.Description));
        throw new ValidationException(failures);
    }

    public async Task DeactivateUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
    }
}
