# CliniSys — Appointment Status Transitions & Edit Dropdowns Spec

Date: 2026-08-13
Status: Draft
Issues: [#32](https://github.com/willianbrecher/clinisys/issues/32), [#33](https://github.com/willianbrecher/clinisys/issues/33)

## 1. Goal

Two appointment-editing bugs, bundled because they both touch
`frontend/src/features/appointments/AppointmentFormContent.tsx`, though each still gets its own
branch/PR (and #32 is backend + frontend, so it splits into two PRs on its own, per root
`CLAUDE.md`):

1. Updating an appointment's status fails unconditionally, including on a plain no-op submit (#32).
2. Editing/viewing an appointment shows blank Patient/Doctor dropdowns instead of the actual
   patient/doctor (#33).

## 2. #32 — Appointment status transitions

### Current behavior — confirmed

`UpdateAppointmentStatusCommandHandler.cs:26-39`
(`backend/src/CliniSys.Application/Commands/Appointments/UpdateAppointmentStatus/`) only allows six
explicit `(from, to)` transitions:

```csharp
var valid = (appointment.Status, request.Status) switch
{
    (AppointmentStatus.Scheduled,  AppointmentStatus.Confirmed)  => true,
    (AppointmentStatus.Scheduled,  AppointmentStatus.Cancelled)  => true,
    (AppointmentStatus.Confirmed,  AppointmentStatus.Completed)  => true,
    (AppointmentStatus.Confirmed,  AppointmentStatus.Cancelled)  => true,
    (AppointmentStatus.Confirmed,  AppointmentStatus.NoShow)     => true,
    (AppointmentStatus.Scheduled,  AppointmentStatus.NoShow)     => true,
    _ => false
};

if (!valid)
    throw new ConflictException($"Cannot transition from {appointment.Status} to {request.Status}.");
```

Two concrete gaps, confirmed by reading the full status-update path (no `StartsAt`/`DateTime.UtcNow`
check exists anywhere in it — the bug is unconditional, not past/future-dependent):

- **No same-status (no-op) case.** The status `<select>` (`AppointmentFormContent.tsx:39-42`)
  defaults to `appointment?.status`, so opening the status editor and clicking "Update Status"
  without touching the dropdown sends e.g. `Scheduled → Scheduled`, which isn't in the switch and
  throws `ConflictException` — reproduces every time, on any appointment.
- **No `Scheduled → Completed`.** Only `Confirmed → Completed` is allowed, so a `Scheduled`
  appointment can never be marked `Completed` directly.

`ConflictException` maps to HTTP 409 with body `{ "message": "Cannot transition from X to Y." }`
(`backend/src/CliniSys.Api/Middleware/ExceptionMiddleware.cs:36,44-51`). The frontend currently
discards that message:

```tsx
const onStatusSubmit = async (data: StatusFormData) => {
  try {
    await updateAppointmentStatus(appointment!.id, data.status as AppointmentStatus);
    toast.success("Status updated.");
    onSaved();
    onClose();
  } catch {
    toast.error("Failed to update status.");
  }
};
```

The codebase already has a shared helper for exactly this — `getApiErrorMessage`
(`frontend/src/lib/apiError.ts:3-10`), already used the same way in `DoctorFormContent.tsx:28` and
three call sites in `UsersPage.tsx` (`:32,37,48`).

### Proposed fix

**Backend** (`UpdateAppointmentStatusCommandHandler.cs`) — add two cases to the switch:

```csharp
var valid = (appointment.Status, request.Status) switch
{
    var (from, to) when from == to                               => true, // no-op
    (AppointmentStatus.Scheduled,  AppointmentStatus.Confirmed)  => true,
    (AppointmentStatus.Scheduled,  AppointmentStatus.Cancelled)  => true,
    (AppointmentStatus.Scheduled,  AppointmentStatus.Completed)  => true,
    (AppointmentStatus.Confirmed,  AppointmentStatus.Completed)  => true,
    (AppointmentStatus.Confirmed,  AppointmentStatus.Cancelled)  => true,
    (AppointmentStatus.Confirmed,  AppointmentStatus.NoShow)     => true,
    (AppointmentStatus.Scheduled,  AppointmentStatus.NoShow)     => true,
    _ => false
};
```

When `from == to`, the handler still runs `appointment.Status = request.Status;` and saves — a
harmless no-op write, simplest correct behavior (no special early-return branch needed).

**Frontend** (`AppointmentFormContent.tsx`) — use the existing helper instead of a bare `catch`:

```tsx
} catch (err) {
  toast.error(getApiErrorMessage(err, "Failed to update status."));
}
```

plus the matching import: `import { getApiErrorMessage } from "@/lib/apiError";`.

### Open question — deliberately not deciding here

Whether transitions out of terminal states (`Completed`/`Cancelled`/`NoShow`) or "un-confirming"
(`Confirmed → Scheduled`) should ever be allowed is a business-rules question, not a bug-fix
question — the issue only asks that no-op submits and `Scheduled → Completed` stop failing.
**Leaving all other currently-blocked transitions blocked** (they'll still 409, now with the real
message surfaced instead of a generic one). If more transitions should open up, that's a separate
issue/spec.

## 3. #33 — Blank patient/doctor dropdowns on edit

### Current behavior — confirmed

Two independent mount effects in `AppointmentFormContent.tsx`:

```tsx
useEffect(() => {                                                    // :44-47
  getPatients({ pageSize: 100 }).then((r) => setPatients(r.items)).catch(() => {});
  getDoctors({ pageSize: 100 }).then((r) => setDoctors(r.items)).catch(() => {});
}, []);

useEffect(() => {                                                    // :49-61
  if (appointment) {
    reset({
      patientId: appointment.patientId,
      doctorId: appointment.doctorId,
      startsAt: appointment.startsAt.slice(0, 16),
      durationMinutes: appointment.durationMinutes,
      notes: appointment.notes ?? "",
    });
  } else {
    reset({ startsAt: defaultStartsAt?.slice(0, 16) ?? "", durationMinutes: 30 });
  }
}, [appointment, defaultStartsAt, reset]);
```

Both run on mount in declaration order; the fetch effect's promises can't resolve before the reset
effect's synchronous body completes, so `reset()` always fires while `patients`/`doctors` are still
`[]`. RHF's `reset()` sets the native `<select>`'s `.value` via ref — assigning a UUID with no
matching `<option>` yet is a no-op the browser doesn't retroactively apply once the real
`<option>`s render in. Deterministic every time, not a network-speed race.

Both selects are `disabled` in edit/detail mode (`:112`, `:125`), so there's no way for the user to
work around it by re-picking manually.

### Proposed fix

Gate the reset on both fetches having completed, using `Promise.all` plus a small `optionsLoaded`
flag, replacing the two independent effects:

```tsx
const [optionsLoaded, setOptionsLoaded] = useState(false);

useEffect(() => {
  Promise.all([
    getPatients({ pageSize: 100 }).then((r) => setPatients(r.items)).catch(() => {}),
    getDoctors({ pageSize: 100 }).then((r) => setDoctors(r.items)).catch(() => {}),
  ]).finally(() => setOptionsLoaded(true));
}, []);

useEffect(() => {
  if (!optionsLoaded) return;
  if (appointment) {
    reset({
      patientId: appointment.patientId,
      doctorId: appointment.doctorId,
      startsAt: appointment.startsAt.slice(0, 16),
      durationMinutes: appointment.durationMinutes,
      notes: appointment.notes ?? "",
    });
  } else {
    reset({ startsAt: defaultStartsAt?.slice(0, 16) ?? "", durationMinutes: 30 });
  }
}, [optionsLoaded, appointment, defaultStartsAt, reset]);
```

`.finally` runs regardless of either fetch failing (both already swallow their own errors via
`.catch(() => {})`), so a failed patients/doctors load still unblocks the reset instead of hanging
it forever with the dropdowns permanently disabled-and-empty.

`reset()` now fires exactly once (not once-too-early plus never-corrected), so there's no risk of
it re-clobbering in-progress edits — it only runs after `optionsLoaded` flips true, which happens
once.

**Trade-off, accepted:** for the "new appointment" (non-edit) case, the `startsAt`/`durationMinutes`
defaults are now also delayed until both fetches resolve (typically well under a second), instead
of being set immediately on mount. This is a minor, one-time delay, not a regression worth avoiding
with extra branching — both effects were already coupled by the same underlying data-readiness
requirement.

## 4. Non-goals

- #32: no change to which transitions are business-valid beyond the two additions above — see
  "Open question" in §3.
- #32: no change to `ConflictException`/`ExceptionMiddleware` response shape — already correct and
  already has a working frontend consumer pattern (`getApiErrorMessage`).
- #33: no change to the `disabled` state of the patient/doctor selects — they stay
  non-editable on reschedule/detail, per existing design; this fix only makes the pre-filled value
  actually render.
- #33: no visual/loading-state change (e.g. a spinner while `optionsLoaded` is false) — out of
  scope, the delay is sub-second and not worth extra UI for.
