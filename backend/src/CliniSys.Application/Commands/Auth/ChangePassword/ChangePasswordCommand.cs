using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Auth.ChangePassword;

/// <summary>Command for a user to change their own password.</summary>
/// <param name="UserId">The calling user's identifier.</param>
/// <param name="CurrentPassword">Current plain-text password for verification.</param>
/// <param name="NewPassword">New plain-text password.</param>
public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : ICommand<Unit>;
