# CliniSys — Fix GET /api/account/me Preference Staleness Spec

Date: 2026-08-17
Status: Draft
Issue: [#40](https://github.com/willianbrecher/clinisys/issues/40)

## 1. Goal

Theme/language changes made on the Account page should survive a page refresh. Today they revert
to whatever was active at login, because `GET /api/account/me` reads `theme`/`language` from the
JWT's claims — which are only set once at login — instead of from the database, even though saving
a preference change does correctly update the database.

## 2. Current behavior — confirmed

`AccountController.Me()` (`backend/src/CliniSys.Api/Controllers/AccountController.cs:26-35`)
builds its entire response from token claims, no database access at all:

```csharp
[HttpGet("me")]
public IActionResult Me() => Ok(new
{
    userId   = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(Claims.Subject),
    role     = User.FindFirstValue("role"),
    fullName = User.FindFirstValue("fullName"),
    theme    = User.FindFirstValue("theme"),
    language = User.FindFirstValue("language"),
    doctorId = User.FindFirstValue("doctorId"),
});
```

The `theme`/`language` claims are set exactly once, at login
(`backend/src/CliniSys.Api/Controllers/ConnectController.cs:68-69`:
`.SetClaim("theme", user.ThemePreference.ToString()).SetClaim("language", user.LanguagePreference)`)
and never refreshed — the JWT itself doesn't change until the user logs in again.

Saving a preference change works correctly: `AccountController.UpdatePreferences` (`:53-59`)
dispatches `UpdatePreferencesCommand`, whose handler
(`UpdatePreferencesCommandHandler.cs:20-29`) loads the user via
`IUserRepository.GetByIdAsync`, sets `ThemePreference`/`LanguagePreference`, and calls
`SaveChangesAsync` — the database row is correct immediately.

`frontend/src/auth/AuthContext.tsx` calls `getMe()` on mount (`:36-47`) and on login (`:49-55`),
then `applyPreferences(me.theme, me.language, setTheme)` — overwriting live theme/i18n state with
whatever `/me` returns. Since `/me` returns the token's stale claim, any preference change reverts
on the next page load, and only "sticks" after a full logout/login issues a fresh token.

## 3. Proposed fix

Read `theme`/`language` from the database in `Me()`, mirroring the existing `GetPatientById`/
`GetDoctorById` dedicated-query pattern already established in this codebase, and reusing
`IUserRepository.GetByIdAsync` (the same repository call `UpdatePreferencesCommandHandler` already
uses to write these two fields).

New files:

`backend/src/CliniSys.Application/Queries/Account/GetCurrentUserPreferences/GetCurrentUserPreferencesQuery.cs`:

```csharp
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
```

`backend/src/CliniSys.Application/Queries/Account/GetCurrentUserPreferences/GetCurrentUserPreferencesQueryHandler.cs`:

```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;

namespace CliniSys.Application.Queries.Account.GetCurrentUserPreferences;

/// <summary>Handler for <see cref="GetCurrentUserPreferencesQuery"/>.</summary>
public class GetCurrentUserPreferencesQueryHandler
    : IQueryHandler<GetCurrentUserPreferencesQuery, CurrentUserPreferencesModel?>
{
    private readonly IUserRepository _users;
    /// <summary>Initialises the handler.</summary>
    /// <param name="users">User repository.</param>
    public GetCurrentUserPreferencesQueryHandler(IUserRepository users) => _users = users;

    /// <summary>Returns the user's current preferences, or <see langword="null"/> if the user no longer exists.</summary>
    /// <param name="request">Query with the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The preferences, or <see langword="null"/>.</returns>
    public async Task<CurrentUserPreferencesModel?> Handle(
        GetCurrentUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        return user is null ? null : new CurrentUserPreferencesModel(user.ThemePreference, user.LanguagePreference);
    }
}
```

`AccountController.Me()` becomes async and dispatches the query, falling back to the token claim
only in the (practically unreachable while the token is valid) case the user record is gone:

```csharp
[HttpGet("me")]
public async Task<IActionResult> Me(CancellationToken ct)
{
    var prefs = await _mediator.Send(new GetCurrentUserPreferencesQuery(CurrentUserId), ct);
    return Ok(new
    {
        userId   = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(Claims.Subject),
        role     = User.FindFirstValue("role"),
        fullName = User.FindFirstValue("fullName"),
        theme    = prefs?.Theme.ToString() ?? User.FindFirstValue("theme"),
        language = prefs?.Language ?? User.FindFirstValue("language"),
        doctorId = User.FindFirstValue("doctorId"),
    });
}
```

`prefs.Theme.ToString()` produces the identical string format (`"Light"`/`"Dark"`/`"System"`) the
claim already used (`user.ThemePreference.ToString()` in `ConnectController.cs:68`) — no frontend
change needed; `AuthContext.tsx`'s `applyPreferences(me.theme, me.language, setTheme)` keeps working
unchanged.

## 4. Non-goals

- `userId`/`role`/`fullName`/`doctorId` stay claim-based. Only `theme`/`language` have a live
  post-login update path (`PATCH /api/account/preferences`); the others don't change without a
  fresh login already (role changes are admin-only and out of scope, name-change isn't a feature
  in this app, `doctorId` is immutable once assigned). Making all five DB-backed would be
  unrelated scope creep — the issue's suggested fix explicitly scopes this to `theme`/`language`.
- No change to how preferences are *saved* — `UpdatePreferencesCommand`/`Handler` are already
  correct and untouched.
- No frontend changes — the response shape and values are unchanged in the normal case; the fix is
  entirely in what backs the response server-side.
