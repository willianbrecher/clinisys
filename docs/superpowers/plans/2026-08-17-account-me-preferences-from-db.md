# Fix GET /api/account/me Preference Staleness Implementation Plan

> Implement task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop theme/language preference changes from reverting on refresh (#40) by having
`GET /api/account/me` read them from the database instead of the JWT's (login-time-only) claims.
Spec: `docs/superpowers/specs/2026-08-17-account-me-preferences-from-db.md`.

**Tech Stack:** .NET 8/C# 12, MediatR (backend only — no frontend changes needed).

## Global Constraints

- Follow root `CLAUDE.md`: branch `fix/<slug>` referencing issue #40.
- Single-layer (backend-only) change → PR uses `Closes #40`.
- Repo has a `bug` label matching the issue's own label — use it.

---

### Task 1: Read theme/language from the database in `Me()` (#40)

**Branch:** `fix/account-me-preferences-from-db` → PR `Closes #40`

**Files:**
- Add: `backend/src/CliniSys.Application/Queries/Account/GetCurrentUserPreferences/GetCurrentUserPreferencesQuery.cs`
- Add: `backend/src/CliniSys.Application/Queries/Account/GetCurrentUserPreferences/GetCurrentUserPreferencesQueryHandler.cs`
- Modify: `backend/src/CliniSys.Api/Controllers/AccountController.cs`

**Interfaces:** new `GetCurrentUserPreferencesQuery : IQuery<CurrentUserPreferencesModel?>` (public,
dispatched by the controller only). No repository interface changes — reuses
`IUserRepository.GetByIdAsync`, the same call `UpdatePreferencesCommandHandler` already uses to
write these two fields.

- [ ] **Step 1: Add `GetCurrentUserPreferencesQuery`**

Create `backend/src/CliniSys.Application/Queries/Account/GetCurrentUserPreferences/GetCurrentUserPreferencesQuery.cs`:

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

- [ ] **Step 2: Add `GetCurrentUserPreferencesQueryHandler`**

Create `backend/src/CliniSys.Application/Queries/Account/GetCurrentUserPreferences/GetCurrentUserPreferencesQueryHandler.cs`:

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

- [ ] **Step 3: Update `AccountController.Me()` to dispatch the new query**

In `backend/src/CliniSys.Api/Controllers/AccountController.cs`:

1. Add to the `using` list:

```csharp
using CliniSys.Application.Queries.Account.GetCurrentUserPreferences;
```

2. Replace (`:26-35`):

```csharp
/// <summary>Returns the current user's profile info from their token claims.</summary>
/// <returns>User identity payload.</returns>
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

with:

```csharp
/// <summary>Returns the current user's profile info — theme/language from the database (so a
/// preference change survives refresh without a fresh login), the rest from token claims.</summary>
/// <param name="ct">Cancellation token.</param>
/// <returns>User identity payload.</returns>
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

- [ ] **Step 4: Build and manually verify**

- `dotnet build` on the backend solution — confirms the new query/handler wire up correctly via
  MediatR's assembly scanning, no DI changes needed.
- Log in, note the current theme/language shown in `GET /api/account/me`'s response.
- `PATCH /api/account/preferences` with a different theme/language, then call `GET /api/account/me`
  again *without* logging out — response now reflects the new values (previously would have kept
  showing the login-time values).
- Refresh the app in the browser after changing a preference (no logout) — theme/language stay as
  last set instead of reverting.
- `userId`/`role`/`fullName`/`doctorId` in the response are unchanged (still claim-based) —
  confirms the fix is scoped to `theme`/`language` only.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CliniSys.Application/Queries/Account/GetCurrentUserPreferences backend/src/CliniSys.Api/Controllers/AccountController.cs
git commit -m "fix: read theme/language preferences from the database in GET /api/account/me"
```

- [ ] **Step 6: Open PR**

```bash
gh pr create --title "fix: read theme/language preferences from the database in GET /api/account/me" \
  --body "Closes #40

\`AccountController.Me()\` built its response entirely from JWT claims, including \`theme\`/\`language\` — but those claims are set once at login and never refreshed, so a preference change (which does correctly update the database via \`PATCH /api/account/preferences\`) reverted on the next page load/refresh until the user logged out and back in. Adds a dedicated \`GetCurrentUserPreferencesQuery\`/\`GetCurrentUserPreferencesQueryHandler\` reading \`ThemePreference\`/\`LanguagePreference\` from the database (reusing \`IUserRepository.GetByIdAsync\`, the same call the preferences-update handler already uses), and wires it into \`Me()\` for just those two fields — \`userId\`/\`role\`/\`fullName\`/\`doctorId\` stay claim-based since they have no live post-login update path.

Spec: \`docs/superpowers/specs/2026-08-17-account-me-preferences-from-db.md\`" \
  --label bug --assignee willianbrecher
```
