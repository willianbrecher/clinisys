using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.DeactivateUser;

/// <summary>Command to lock a user account indefinitely.</summary>
/// <param name="Id">User identifier.</param>
public record DeactivateUserCommand(Guid Id) : ICommand<Unit>;
