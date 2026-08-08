# Bug Fix Batch 1 Implementation Plan

> Implement task-by-task, in order. Steps use checkbox (`- [ ]`) syntax for tracking. Each task ends with its own commit, per the branch/PR split defined below.

**Goal:** Fix five bugs from first-usage feedback: doctor edit form not loading its current value (#1), generic/opaque errors on appointment save (#8) and password reset (#10), no active/inactive visibility or reactivation for users (#9), and misaligned sidebar/header (#13). Spec: `docs/superpowers/specs/2026-05-08-bugfix-batch-1.md`.

**Tech Stack:** .NET 8 / C# 12, MediatR, FluentValidation, ASP.NET Core Identity (backend); React 18, TypeScript, react-hook-form, axios, react-i18next (frontend).

## Global Constraints

- Follow root `CLAUDE.md`: one branch per fix (`fix/<slug>`), referencing its issue. Backend and frontend changes for the same issue get **separate PRs** (Task 4 spans both layers).
- PR issue linkage: single-layer fixes (#1, #8, #10, #13) use `Closes #N`. #9's two PRs (backend, frontend) both use `Refs #9` — close #9 manually once both merge.
- No test project exists yet in this repo — do not add one as part of this batch (see spec §6, non-goals).
- Keep `en-US`/`pt-BR`/`es-ES` locale files in sync for every new user-facing string.

---

### Task 1: Doctor edit form — fetch failure (#1)

**Branch:** `fix/doctor-edit-stale-fetch` → PR `Closes #1`

**Status: done.** The originally-planned frontend-only fetch-race guard turned out not to be the root cause — manual repro after applying it surfaced the real bug (backend), described below. Recorded here for the actual history; see spec §3 for full detail.

**Files:**
- Modify: `frontend/src/features/doctors/DoctorFormContent.tsx` (defensive improvement, not the fix)
- Create: `frontend/src/lib/apiError.ts` (pulled forward from Task 2 — required for the above)
- Modify: `backend/src/CliniSys.Api/Controllers/DoctorsController.cs`
- Modify: `backend/src/CliniSys.Application/Common/Interfaces/Repositories/IDoctorRepository.cs`
- Modify: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/DoctorRepository.cs`
- Create: `backend/src/CliniSys.Application/Queries/Doctors/GetDoctorById/GetDoctorByIdQuery.cs`
- Create: `backend/src/CliniSys.Application/Queries/Doctors/GetDoctorById/GetDoctorByIdQueryHandler.cs`

**What actually happened:**

1. Applied the planned frontend guard (staleness flag + error surfacing via `getApiErrorMessage`) in `DoctorFormContent.tsx`. This didn't fix the bug, but turned a silent failure into a visible "Validation failed." toast on every doctor edit.
2. That toast revealed the real bug: `DoctorsController.GetById` called `GetDoctorsQuery(1, 1000)`, but `GetDoctorsQueryHandler` throws `ValidationException` for any `PageSize > 100`. `GetById` failed unconditionally, for every doctor, every time.
3. Fixed by adding a proper single-record path instead of reusing the capped list query:
   - `IDoctorRepository.GetByIdWithUserAsync(Guid id, CancellationToken ct)` — `_set.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id, ct)`.
   - `GetDoctorByIdQuery(Guid Id) : IQuery<DoctorModel?>` + `GetDoctorByIdQueryHandler`, calling the new repo method and mapping to the existing `DoctorModel` record from `GetDoctors`.
   - `DoctorsController.GetById` now sends `GetDoctorByIdQuery` instead of paging through `GetDoctorsQuery(1, 1000)`.
4. Verified with `dotnet build` (0 errors) — no running dev server / browser repro available in this environment; **still needs a live check** (open the doctor edit dialog and confirm the specialty loads, and that `PATCH` still works).

**Related, not fixed here:** `PatientsController.GetById` has the identical bug (`GetPatientsQuery(null, 1, 1000)` against the same `PageSize > 100` cap in `GetPatientsQueryHandler`) — patient editing likely fails the same way. Out of scope for #1; file a separate issue.

- [ ] **Commit** (backend fix, plus the earlier frontend commit already made)

```bash
git add backend/src/CliniSys.Api/Controllers/DoctorsController.cs \
        backend/src/CliniSys.Application/Common/Interfaces/Repositories/IDoctorRepository.cs \
        backend/src/CliniSys.Infrastructure/Persistence/Repositories/DoctorRepository.cs \
        backend/src/CliniSys.Application/Queries/Doctors/GetDoctorById
git commit -m "fix: give doctors a proper GetById query instead of a capped list scan"
```

- [ ] **Live verification still needed:** run the app (`run` skill or manually), open the doctor edit dialog for at least two different doctors, confirm the specialty field loads correctly each time, and confirm saving still works.

---

### Task 2: Shared API error helper + appointment save errors (#8)

**Branch:** `fix/appointment-save-error-message` → PR `Closes #8`

**Files:**
- Create: `frontend/src/lib/apiError.ts`
- Modify: `frontend/src/features/appointments/AppointmentFormContent.tsx`

**Interfaces:**
- Produces: `getApiErrorMessage(err: unknown, fallback: string): string` from `@/lib/apiError` — consumed here and by Task 3 (and optionally Task 1).

- [ ] **Step 1: Create `frontend/src/lib/apiError.ts`**

```ts
import { isAxiosError } from "axios";

export function getApiErrorMessage(err: unknown, fallback: string): string {
  if (isAxiosError(err)) {
    const data = err.response?.data as { message?: string; errors?: string[] } | undefined;
    if (data?.errors?.length) return data.errors.join(" ");
    if (data?.message) return data.message;
  }
  return fallback;
}
```

- [ ] **Step 2: Use it in `AppointmentFormContent.onSubmit`**

In `frontend/src/features/appointments/AppointmentFormContent.tsx`, add the import:

```ts
import { getApiErrorMessage } from "@/lib/apiError";
```

Change the catch block:

```ts
} catch {
  toast.error("Failed to save appointment.");
}
```

to:

```ts
} catch (err) {
  toast.error(getApiErrorMessage(err, "Failed to save appointment."));
}
```

Do the same for `onStatusSubmit`'s catch block (same file) for consistency, since it hits the same backend error contract:

```ts
} catch (err) {
  toast.error(getApiErrorMessage(err, "Failed to update status."));
}
```

- [ ] **Step 3: Manually verify**

Via the `run` skill: try to create an appointment that conflicts with clinic open hours and one that overlaps an existing appointment for the same doctor. Confirm the toast now shows the specific backend message (e.g. "The doctor already has an appointment at that time.") instead of the generic fallback.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/lib/apiError.ts frontend/src/features/appointments/AppointmentFormContent.tsx
git commit -m "fix: surface backend error messages on appointment save"
```

---

### Task 3: Reset password error message (#10)

**Branch:** `fix/reset-password-error-message` → PR `Closes #10`

**Files:**
- Modify: `frontend/src/features/users/UsersPage.tsx`

**Interfaces:** consumes `getApiErrorMessage` from Task 2 — do this task after Task 2 lands, or rebase onto it.

- [ ] **Step 1: Use the helper in `handleResetPw`**

In `frontend/src/features/users/UsersPage.tsx`, add the import:

```ts
import { getApiErrorMessage } from "@/lib/apiError";
```

Change:

```ts
const handleResetPw = async () => {
  if (!resetTarget || !newPw) return;
  try {
    await resetPassword(resetTarget.id, newPw);
    toast.success("Password reset.");
    setResetTarget(null);
    setNewPw("");
  } catch { toast.error("Failed to reset password."); }
};
```

to:

```ts
const handleResetPw = async () => {
  if (!resetTarget || !newPw) return;
  try {
    await resetPassword(resetTarget.id, newPw);
    toast.success("Password reset.");
    setResetTarget(null);
    setNewPw("");
  } catch (err) {
    toast.error(getApiErrorMessage(err, "Failed to reset password."));
  }
};
```

- [ ] **Step 2: Manually verify**

Via the `run` skill: as Admin, reset a user's password to something that fails Identity's complexity rules (e.g. `"abc"`, below the client-side 8-char minimum bypassed by editing devtools, or just confirm the client-side `newPw.length < 8` guard first — then test a password that's 8+ chars but still fails server-side rules if any additional rules exist beyond length). Confirm the toast shows the specific rule violation instead of "Failed to reset password."

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/users/UsersPage.tsx
git commit -m "fix: surface backend validation errors on password reset"
```

---

### Task 4: User active/inactive status + reactivate (#9)

Two PRs — backend first, frontend depends on it.

#### 4a. Backend

**Branch:** `fix/user-reactivate-backend` → PR `Refs #9`

**Files:**
- Modify: `backend/src/CliniSys.Application/Common/Interfaces/IIdentityService.cs`
- Modify: `backend/src/CliniSys.Infrastructure/Identity/IdentityService.cs`
- Modify: `backend/src/CliniSys.Application/Queries/Users/GetUsers/GetUsersQueryHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Users/ReactivateUser/ReactivateUserCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Users/ReactivateUser/ReactivateUserCommandHandler.cs`
- Modify: `backend/src/CliniSys.Api/Controllers/UsersController.cs`

**Interfaces:**
- Produces: `UserModel.IsActive: bool`; `PATCH /api/users/{id}/reactivate`.

- [ ] **Step 1: Add `ReactivateUserAsync` to `IIdentityService`**

In `backend/src/CliniSys.Application/Common/Interfaces/IIdentityService.cs`, add after `DeactivateUserAsync`:

```csharp
/// <summary>Clears an indefinite lockout, restoring the user's ability to sign in.</summary>
/// <param name="userId">User identifier.</param>
/// <param name="ct">Cancellation token.</param>
Task ReactivateUserAsync(Guid userId, CancellationToken ct = default);
```

- [ ] **Step 2: Implement it in `IdentityService`**

In `backend/src/CliniSys.Infrastructure/Identity/IdentityService.cs`, add after `DeactivateUserAsync`:

```csharp
public async Task ReactivateUserAsync(Guid userId, CancellationToken ct = default)
{
    var user = await _userManager.FindByIdAsync(userId.ToString())
        ?? throw new InvalidOperationException("User not found.");
    await _userManager.SetLockoutEndDateAsync(user, null);
}
```

- [ ] **Step 3: Add `ReactivateUserCommand`**

`backend/src/CliniSys.Application/Commands/Users/ReactivateUser/ReactivateUserCommand.cs`:

```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.ReactivateUser;

/// <summary>Command to clear a user's account lockout.</summary>
/// <param name="Id">User identifier.</param>
public record ReactivateUserCommand(Guid Id) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Users/ReactivateUser/ReactivateUserCommandHandler.cs`:

```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.ReactivateUser;

/// <summary>Handler for <see cref="ReactivateUserCommand"/>.</summary>
public class ReactivateUserCommandHandler : ICommandHandler<ReactivateUserCommand, Unit>
{
    private readonly IIdentityService _identity;
    /// <summary>Initialises the handler.</summary>
    /// <param name="identity">Identity service.</param>
    public ReactivateUserCommandHandler(IIdentityService identity) => _identity = identity;

    /// <summary>Clears the user's lockout.</summary>
    /// <param name="request">Reactivation command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        await _identity.ReactivateUserAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 4: Add `IsActive` to `UserModel` and compute it in the handler**

In `backend/src/CliniSys.Application/Queries/Users/GetUsers/GetUsersQueryHandler.cs`, change the record:

```csharp
public record UserModel(Guid Id, string? Email, string FullName, Role Role,
    ThemePreference ThemePreference, string LanguagePreference, bool IsActive);
```

and the mapping in `Handle`:

```csharp
var items = paged.Items.Select(u =>
    new UserModel(u.Id, u.Email, u.FullName, u.Role, u.ThemePreference, u.LanguagePreference,
        IsActive: !(u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow)))
    .ToList();
```

Confirmed: `ApplicationUser : IdentityUser<Guid>` (`backend/src/CliniSys.Domain/Entities/ApplicationUser.cs`), so `LockoutEnabled`/`LockoutEnd` are directly available on `u` — no extra mapping needed.

- [ ] **Step 5: Add the endpoint**

In `backend/src/CliniSys.Api/Controllers/UsersController.cs`, add the using and endpoint after `Deactivate`:

```csharp
using CliniSys.Application.Commands.Users.ReactivateUser;
```

```csharp
/// <summary>Clears a user account's lockout.</summary>
/// <param name="id">User identifier.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>No content.</returns>
[HttpPatch("{id:guid}/reactivate")]
public async Task<IActionResult> Reactivate([FromRoute] Guid id, CancellationToken ct)
{
    await _mediator.Send(new ReactivateUserCommand(id), ct);
    return NoContent();
}
```

- [ ] **Step 6: Build and manually verify via Swagger**

```bash
cd backend && dotnet build
```

Run the API (`dotnet run` from `CliniSys.Api`), hit `GET /api/users` via Swagger and confirm `isActive` appears (camelCase per the API's JSON settings), then `PATCH /api/users/{id}/deactivate` followed by `PATCH /api/users/{id}/reactivate` and confirm `isActive` flips accordingly on a subsequent `GET`.

- [ ] **Step 7: Commit**

```bash
git add backend/src/CliniSys.Application/Common/Interfaces/IIdentityService.cs \
        backend/src/CliniSys.Infrastructure/Identity/IdentityService.cs \
        backend/src/CliniSys.Application/Queries/Users/GetUsers/GetUsersQueryHandler.cs \
        backend/src/CliniSys.Application/Commands/Users/ReactivateUser \
        backend/src/CliniSys.Api/Controllers/UsersController.cs
git commit -m "feat: expose user active status and add reactivate endpoint"
```

#### 4b. Frontend

**Branch:** `fix/user-reactivate-frontend` → PR `Refs #9` (depends on 4a merged; targets `master` but requires the backend endpoint to be running to test against)

**Files:**
- Modify: `frontend/src/api/types.ts`
- Modify: `frontend/src/api/users.ts`
- Modify: `frontend/src/features/users/UsersPage.tsx`
- Modify: `frontend/src/locales/en-US/translation.json`
- Modify: `frontend/src/locales/pt-BR/translation.json`
- Modify: `frontend/src/locales/es-ES/translation.json`

- [ ] **Step 1: Add `isActive` to `UserModel`**

In `frontend/src/api/types.ts`, add to the `UserModel` interface:

```ts
export interface UserModel {
  id: string;
  email?: string;
  fullName: string;
  role: Role;
  themePreference: ThemePreference;
  languagePreference: string;
  isActive: boolean;
}
```

- [ ] **Step 2: Add `reactivateUser` to the API module**

In `frontend/src/api/users.ts`, add alongside `deactivateUser`:

```ts
export const reactivateUser = (id: string) => client.patch(`/api/users/${id}/reactivate`);
```

(Match the exact existing `deactivateUser` signature/pattern in that file.)

- [ ] **Step 3: Add locale keys**

`frontend/src/locales/en-US/translation.json`, in `"users"`, after `"deactivate": "Deactivate",`:

```json
"reactivate": "Reactivate",
"statusActive": "Active",
"statusInactive": "Inactive",
```

`frontend/src/locales/pt-BR/translation.json`, same position:

```json
"reactivate": "Reativar",
"statusActive": "Ativo",
"statusInactive": "Inativo",
```

`frontend/src/locales/es-ES/translation.json`, same position:

```json
"reactivate": "Reactivar",
"statusActive": "Activo",
"statusInactive": "Inactivo",
```

- [ ] **Step 4: Update `UsersPage.tsx`**

Add imports:

```ts
import { getUsers, deactivateUser, reactivateUser, resetPassword } from "@/api/users";
import { getApiErrorMessage } from "@/lib/apiError";
```

Add a handler alongside `handleDeactivate`:

```ts
const handleReactivate = async (id: string) => {
  try { await reactivateUser(id); toast.success("User reactivated."); load(); }
  catch (err) { toast.error(getApiErrorMessage(err, "Failed to reactivate user.")); }
};
```

While touching `handleDeactivate`, apply the same error-surfacing fix for consistency (it currently also swallows the message):

```ts
const handleDeactivate = async (id: string) => {
  try { await deactivateUser(id); toast.success("User deactivated."); load(); }
  catch (err) { toast.error(getApiErrorMessage(err, "Failed to deactivate user.")); }
};
```

Add a Status column to the desktop table header:

```tsx
<TableHead>{t("users.role")}</TableHead>
<TableHead>{t("common.status")}</TableHead>
<TableHead>{t("common.actions")}</TableHead>
```

(`common.status` already exists — confirmed in `frontend/src/locales/en-US/translation.json` under `"common"`.)

Add the cell and swap the action button per row (desktop table body):

```tsx
<TableCell>{t(`users.role_${u.role}`)}</TableCell>
<TableCell>
  <span className={u.isActive ? "text-emerald-600" : "text-muted-foreground"}>
    {u.isActive ? t("users.statusActive") : t("users.statusInactive")}
  </span>
</TableCell>
<TableCell>
  <div className="flex gap-2">
    <Button size="sm" variant="outline" onClick={() => setResetTarget(u)}>{t("users.resetPassword")}</Button>
    {u.isActive
      ? <Button size="sm" variant="destructive" onClick={() => handleDeactivate(u.id)}>{t("users.deactivate")}</Button>
      : <Button size="sm" onClick={() => handleReactivate(u.id)}>{t("users.reactivate")}</Button>}
  </div>
</TableCell>
```

Apply the equivalent status line + conditional button in the mobile card block (same file, the `md:hidden` section) — mirror the desktop change: status text under the email/role line, and the same `u.isActive ? deactivate : reactivate` button swap.

- [ ] **Step 5: Manually verify**

Via the `run` skill: confirm the Status column/line shows correctly for active users, deactivate one, confirm it flips to "Inactive" with a "Reactivate" button, click it, confirm it flips back.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/api/types.ts frontend/src/api/users.ts frontend/src/features/users/UsersPage.tsx frontend/src/locales
git commit -m "feat: show user active status and add reactivate action"
```

- [ ] **Step 7: Close #9 manually once both PRs (4a, 4b) have merged** — do not rely on auto-close since both use `Refs #9`.

---

### Task 5: Sidebar/header vertical alignment (#13)

**Branch:** `fix/sidebar-header-alignment` → PR `Closes #13`

**Files:**
- Modify: `frontend/src/components/AppLayout.tsx`

**Interfaces:** none.

- [ ] **Step 1: Match the desktop sidebar header height to the content header**

In `frontend/src/components/AppLayout.tsx`, change:

```tsx
<div className="flex items-center gap-2 px-4 py-4 border-b">
  <LogoMark />
  <span className="font-semibold text-sm">CliniSys</span>
</div>
```

(the first occurrence, inside `<aside>`) to:

```tsx
<div className="flex h-14 items-center gap-2 px-4 border-b">
  <LogoMark />
  <span className="font-semibold text-sm">CliniSys</span>
</div>
```

- [ ] **Step 2: Apply the same change to the mobile `SheetContent` header block**

The second occurrence of the identical `px-4 py-4 border-b` block (inside `<SheetContent>`) must change the same way, so the mobile drawer's header matches too:

```tsx
<div className="flex h-14 items-center gap-2 px-4 border-b">
  <LogoMark />
  <span className="font-semibold text-sm">CliniSys</span>
</div>
```

- [ ] **Step 3: Manually verify visually**

Via the `run` skill: check desktop layout (sidebar header lines up with content header) and confirm the mobile drawer (open the hamburger menu below `lg` breakpoint) still looks correct — the logo should still be vertically centered at the smaller `h-7 w-7` mobile size used elsewhere in the header, not just the desktop `h-8 w-8`.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/AppLayout.tsx
git commit -m "fix: align sidebar header height with content header"
```
