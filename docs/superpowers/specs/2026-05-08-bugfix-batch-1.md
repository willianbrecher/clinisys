# CliniSys — Bug Fix Batch 1 Spec

Date: 2026-08-05
Status: Draft
Issues: [#1](https://github.com/willianbrecher/clinisys/issues/1), [#8](https://github.com/willianbrecher/clinisys/issues/8), [#9](https://github.com/willianbrecher/clinisys/issues/9), [#10](https://github.com/willianbrecher/clinisys/issues/10), [#13](https://github.com/willianbrecher/clinisys/issues/13)

## 1. Goal

Fix five bugs/gaps reported from first real usage of the app, in priority order:

1. Doctor edit form does not load the current specialization value (#1)
2. Cannot save when scheduling a consultation — generic "an error has occurred" message (#8)
3. Reset password only shows a generic error when the new password fails security rules (#10)
4. User list doesn't show active/deactivated status, and there's no way to reactivate a user (#9)
5. Side menu header and content header are not vertically aligned (#13)

Ordering follows the priority the user set (see project memory `project_clinisys_issue_priority`), which does not match issue number order.

## 2. Cross-cutting root cause: swallowed backend error messages (#8, #10)

**Current behavior.** The backend already does the right thing for both of these flows:

- `ExceptionMiddleware` (`backend/src/CliniSys.Api/Middleware/ExceptionMiddleware.cs`) maps `ValidationException`/`ConflictException` to `400`/`409` JSON bodies shaped `{ message, errors? }`.
- `CreateAppointmentCommandHandler` throws `ConflictException` with specific messages ("The clinic is not open on that day.", "The appointment falls outside clinic open hours.", "The doctor already has an appointment at that time.") — `backend/src/CliniSys.Application/Commands/Appointments/CreateAppointment/CreateAppointmentCommandHandler.cs`.
- `IdentityService.ThrowIfFailed` (`backend/src/CliniSys.Infrastructure/Identity/IdentityService.cs`) already converts Identity password-complexity failures into a `ValidationException` with per-rule messages instead of an opaque 500.

The frontend throws all of that away. Every submit handler follows the same pattern:

```ts
catch { toast.error("Failed to save appointment."); }   // AppointmentFormContent.tsx
catch { toast.error("Failed to reset password."); }     // UsersPage.tsx
```

No code anywhere reads `err.response.data.message` or `.errors`. `frontend/src/api/client.ts`'s response interceptor only handles `401`; it does not extract error bodies.

**Proposed fix.** Add one shared helper instead of patching each call site separately, since this exact pattern already recurs at 2+ call sites:

```ts
// frontend/src/lib/apiError.ts
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

Use it at the two reported call sites:

- `AppointmentFormContent.onSubmit` catch block → `toast.error(getApiErrorMessage(err, "Failed to save appointment."))`
- `UsersPage.handleResetPw` catch block → `toast.error(getApiErrorMessage(err, "Failed to reset password."))`

**Out of scope for this batch:** sweeping every other `catch { toast.error(...) }` in the codebase (patients, doctors, users create, clinic settings, etc.) to use the helper. Flagging it here since planning should decide whether to fix opportunistically at these two sites only, or do a repo-wide pass — worth a explicit decision rather than silent scope creep during implementation.

## 3. #1 — Doctor edit form does not load current specialization value

**Current behavior.** `DoctorFormContent` (`frontend/src/features/doctors/DoctorFormContent.tsx`) fetches the doctor and calls `reset({ specialty: d.specialty })` inside a `useEffect` keyed on `[id, reset]`:

```ts
useEffect(() => {
  if (id) getDoctorById(id).then((d) => reset({ specialty: d.specialty })).catch(() => {});
}, [id, reset]);
```

Static review of the form, the `Input` component (plain `forwardRef`, uncontrolled), and the backend (`GET /api/doctors/{id}` → `DoctorsController.GetById` → `GetDoctorsQueryHandler`, which does return `Specialty` correctly) did not surface a deterministic bug — field names match end-to-end (`specialty` on both sides).

**Leading hypothesis: stale-response race.** The fetch has no cancellation or staleness guard. `DoctorsPage` and `DoctorFormContent` stay mounted while only the `:id` route param changes when a user opens one doctor's edit dialog, then — without it fully closing — opens a different doctor's row action. If the first fetch resolves after the second one starts, `reset()` runs with the *first* doctor's data, overwriting the second doctor's freshly-loaded value (or, on a slow connection, the field can also appear to just stay blank if the promise never settles before the user notices). Also silently swallows fetch errors via `.catch(() => {})`, so a failed `GET` fails invisibly and the form is left empty with no error shown.

**Proposed fix.**
- Add a staleness guard in the effect (track the in-flight `id`, discard the result if it no longer matches the current `id` when the promise resolves — the standard `useEffect` async-cleanup pattern).
- Surface fetch failures instead of swallowing them: `catch` → `toast.error(getApiErrorMessage(err, "Failed to load doctor."))` (reusing the helper from §2).

**Verification needed before implementation:** reproduce manually (open doctor A's edit dialog, quickly switch to doctor B via the list) to confirm this is in fact the trigger, since it wasn't confirmed by static analysis alone.

## 4. #9 — User list doesn't show active/deactivated status; no reactivate action

**Current behavior.**
- `ApplicationUser` deactivation is implemented as an ASP.NET Identity lockout, not a domain `IsActive` flag like `Patient`/`Doctor` have: `IdentityService.DeactivateUserAsync` calls `_userManager.SetLockoutEnabledAsync(user, true)` + `SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)`.
- `UserModel` (`GetUsersQueryHandler.cs`) does not include any status field: `record UserModel(Guid Id, string? Email, string FullName, Role Role, ThemePreference ThemePreference, string LanguagePreference)`.
- There is no reactivate command, handler, or endpoint — only `DeactivateUserCommand` / `POST .../deactivate` exist in `UsersController`.
- `UsersPage.tsx` renders a single unconditional "Deactivate" button per row with no status column.

**Proposed fix.**

*Backend:*
- Add `IsActive` (or `IsLockedOut`) to `UserModel`, computed from `LockoutEnabled && LockoutEnd > DateTimeOffset.UtcNow`.
- Add `ReactivateUserCommand`/`ReactivateUserCommandHandler`, mirroring `DeactivateUserCommand` — calls a new `IIdentityService.ReactivateUserAsync(userId)` that clears lockout (`SetLockoutEndDateAsync(user, null)`).
- Add `PATCH /api/users/{id}/reactivate` to `UsersController` (Admin only, consistent with the existing `.../deactivate` endpoint).

*Frontend:*
- Add a Status column (table) / status line (mobile card) to `UsersPage.tsx`, using existing `users.*` locale conventions.
- Add `reactivateUser` to `frontend/src/api/users.ts`.
- Replace the single "Deactivate" button with a conditional Deactivate/Reactivate button based on `u.isActive`.
- Add new locale keys (`users.status_active`, `users.status_inactive`, `users.reactivate`) to all three locale bundles (`en-US`, `pt-BR`, `es-ES`) per this repo's convention of keeping frontend/backend locale sets in sync — see [[project_clinisys_issue_priority]] and `CLAUDE.md`.

## 5. #13 — Side menu header and content header are not vertically aligned

**Current behavior — confirmed root cause.** In `frontend/src/components/AppLayout.tsx`:

- The sidebar's top block is sized implicitly: `<div className="flex items-center gap-2 px-4 py-4 border-b">` wrapping an 8×8 (`h-8 w-8`) logo → `py-4` (1rem top + bottom) + 2rem logo height = **4rem / 64px** (`h-16`) tall.
- The content header is sized explicitly: `<header className="... flex h-14 items-center ...">` = **3.5rem / 56px** tall.

That 8px mismatch between the sidebar's implicit height and the header's explicit `h-14` is the misalignment.

**Proposed fix.** Make both the same explicit height — change the sidebar top block's class from `py-4` to `h-14` (dropping the vertical padding in favor of a fixed height + `items-center`, matching the header), or change the header from `h-14` to `h-16` and adjust the mobile `SheetContent` header block (`frontend/src/components/AppLayout.tsx` line ~98, which shares the same `px-4 py-4 border-b` pattern) to match, whichever height is preferred. Recommend standardizing on `h-14` (56px) since that's the value already used by the main content header and is a more typical top-bar height; verify visually with the `run` skill once implemented, in both desktop and mobile sheet layouts.

## 6. Non-goals for this batch

- No change to the underlying data model for patients/doctors `IsActive` (already exists and is out of scope here).
- No repo-wide error-message-handling sweep beyond the two call sites named in §2 (see explicit out-of-scope note there).
- No new automated tests are added — this repo currently has no backend or frontend test project (see `CLAUDE.md`); adding test infrastructure is a separate, larger decision not bundled into a bug-fix batch.
