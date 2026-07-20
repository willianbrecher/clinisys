using CliniSys.Domain.Enums;

namespace CliniSys.Api.Requests.Account;

/// <summary>HTTP body for PATCH /api/account/preferences.</summary>
/// <param name="Theme">Preferred theme.</param>
/// <param name="Language">BCP-47 language tag.</param>
public record UpdatePreferencesRequest(ThemePreference Theme, string Language);
