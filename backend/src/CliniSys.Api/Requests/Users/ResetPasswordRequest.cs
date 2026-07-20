namespace CliniSys.Api.Requests.Users;

/// <summary>HTTP body for POST /api/users/{id}/reset-password.</summary>
/// <param name="NewPassword">The new password to set.</param>
public record ResetPasswordRequest(string NewPassword);
