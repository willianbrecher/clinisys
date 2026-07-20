using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.ResetPassword;

/// <summary>Command for Admin to reset any user's password without knowing the current one.</summary>
/// <param name="UserId">Target user identifier.</param>
/// <param name="NewPassword">New plain-text password.</param>
public record ResetPasswordCommand(Guid UserId, string NewPassword) : ICommand<Unit>;
