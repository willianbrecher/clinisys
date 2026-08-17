using CliniSys.Application.Common.Interfaces;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Queries.Account.GetCurrentUserPreferences;

/// <summary>Preferences for the current user, read from the database.</summary>
/// <param name="Theme">Theme preference.</param>
/// <param name="Language">BCP-47 language tag.</param>
public record CurrentUserPreferencesModel(ThemePreference Theme, string Language);

/// <summary>Query to fetch a user's current theme/language preferences.</summary>
/// <param name="UserId">User identifier.</param>
public record GetCurrentUserPreferencesQuery(Guid UserId) : IQuery<CurrentUserPreferencesModel?>;
