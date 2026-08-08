using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.ReactivateUser;

/// <summary>Command to clear a user's account lockout.</summary>
/// <param name="Id">User identifier.</param>
public record ReactivateUserCommand(Guid Id) : ICommand<Unit>;
