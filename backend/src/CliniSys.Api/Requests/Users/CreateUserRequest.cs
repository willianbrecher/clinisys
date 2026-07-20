using CliniSys.Domain.Enums;

namespace CliniSys.Api.Requests.Users;

/// <summary>HTTP body for POST /api/users.</summary>
/// <param name="Email">Email address.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Password">Initial password.</param>
/// <param name="Role">User role.</param>
/// <param name="Specialty">Required when Role is Doctor.</param>
public record CreateUserRequest(string Email, string FullName, string Password, Role Role, string? Specialty);
