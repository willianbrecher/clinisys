namespace CliniSys.Api.Requests.Account;

/// <summary>HTTP body for PATCH /api/account/profile-picture.</summary>
/// <param name="ProfilePictureBase64">Base64 data URI or <see langword="null"/> to remove.</param>
public record UpdateProfilePictureRequest(string? ProfilePictureBase64);
