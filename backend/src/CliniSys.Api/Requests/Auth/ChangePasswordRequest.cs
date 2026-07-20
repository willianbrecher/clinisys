namespace CliniSys.Api.Requests.Auth;

/// <summary>HTTP body for POST /api/auth/change-password.</summary>
/// <param name="CurrentPassword">Current password for verification.</param>
/// <param name="NewPassword">New password to set.</param>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
