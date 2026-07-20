using CliniSys.Application.Common.Interfaces;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Commands.Users.CreateUser;

/// <summary>Command to create a new user account. If Role is Doctor, also creates the Doctor profile.</summary>
/// <param name="Email">Email address (used as login username).</param>
/// <param name="FullName">Display name.</param>
/// <param name="Password">Initial plain-text password.</param>
/// <param name="Role">User role.</param>
/// <param name="Specialty">Required when Role is Doctor; ignored otherwise.</param>
public record CreateUserCommand(string Email, string FullName, string Password,
    Role Role, string? Specialty) : ICommand<Guid>;
