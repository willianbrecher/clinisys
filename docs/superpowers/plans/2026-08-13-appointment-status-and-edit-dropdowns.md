# Appointment Status Transitions & Edit Dropdowns Implementation Plan

> Implement task-by-task, in order. Steps use checkbox (`- [ ]`) syntax for tracking. **Merge each
> PR to `master` before starting the next task** — tasks 2 and 3 both touch
> `AppointmentFormContent.tsx` (different regions), so branching each off the latest `master` avoids
> conflicts instead of resolving them later.

**Goal:** Fix appointment status transitions failing unconditionally (#32) and blank patient/doctor
dropdowns on edit (#33).
Spec: `docs/superpowers/specs/2026-08-13-appointment-status-and-edit-dropdowns.md`.

**Tech Stack:** .NET 8/C# 12 (task 1), React 18/TypeScript/React Hook Form (tasks 2-3).

## Global Constraints

- Follow root `CLAUDE.md`: one branch (`fix/<slug>`) per PR, referencing its issue.
- #32 spans backend + frontend → **two PRs, both `Refs #32`** (never `Closes #32` on either —
  close the issue manually once both merge).
- #33 is frontend-only → single PR, `Closes #33`.
- Both issues are `bug`-labeled — use `bug` on all PRs.
- Implementation order: **Task 1 (#32 backend) → Task 2 (#33 dropdowns) → Task 3 (#32 frontend
  error surfacing)**. Task 1 doesn't touch any frontend file, so its order relative to 2/3 doesn't
  matter for conflicts; Task 2 goes before Task 3 only to keep a single clean sequence on the
  shared frontend file (their line ranges don't actually overlap, so this isn't a hard
  dependency).

---

### Task 1: Fix the status-transition table (#32, backend)

**Branch:** `fix/appointment-status-transitions-backend` → PR `Refs #32`

**Files:**
- Modify: `backend/src/CliniSys.Application/Commands/Appointments/UpdateAppointmentStatus/UpdateAppointmentStatusCommandHandler.cs`

**Interfaces:** none — internal handler logic only.

- [ ] **Step 1: Add the no-op and `Scheduled → Completed` cases to the transition switch**

In `UpdateAppointmentStatusCommandHandler.cs:26-35`, replace:

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
```

with:

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

No other change in the handler — the existing `appointment.Status = request.Status; ... SaveChangesAsync(...)` below already handles the no-op case correctly (writes the same value back, harmless).

- [ ] **Step 2: Manually verify** (build/run backend; no automated tests exist for this handler
  per `backend/CLAUDE.md` conventions — confirm via API call or through the UI once Task 3 lands)

- `PATCH /api/appointments/{id}/status` with the appointment's current status → `204 No Content`
  (previously `409`).
- `Scheduled → Completed` → `204 No Content` (previously `409`).
- A genuinely invalid transition (e.g. `Completed → Scheduled`) still → `409` with
  `{ "message": "Cannot transition from Completed to Scheduled." }` — confirms other transitions
  remain blocked as intended.

- [ ] **Step 3: Commit**

```bash
git add backend/src/CliniSys.Application/Commands/Appointments/UpdateAppointmentStatus/UpdateAppointmentStatusCommandHandler.cs
git commit -m "fix: allow no-op and Scheduled-to-Completed appointment status transitions"
```

- [ ] **Step 4: Open PR**

```bash
gh pr create --title "fix: allow no-op and Scheduled-to-Completed appointment status transitions" \
  --body "Refs #32

The status-transition switch in \`UpdateAppointmentStatusCommandHandler\` rejected same-status (no-op) submits and direct \`Scheduled → Completed\`, causing every plain \"Update Status\" click (the dropdown defaults to the current status) to fail with a 409. Adds both cases. All other previously-blocked transitions (e.g. leaving a terminal state, un-confirming) remain blocked — that's a separate business-rules question, not addressed here.

Spec: \`docs/superpowers/specs/2026-08-13-appointment-status-and-edit-dropdowns.md\`" \
  --label bug --assignee willianbrecher
```

---

### Task 2: Fix blank patient/doctor dropdowns on edit (#33)

**Branch:** `fix/appointment-edit-dropdowns` → PR `Closes #33`. Branch from `master` after Task 1's
PR merges.

**Files:**
- Modify: `frontend/src/features/appointments/AppointmentFormContent.tsx`

**Interfaces:** none — internal component state/effects only.

- [ ] **Step 1: Add an `optionsLoaded` flag and gate the fetch effect on `Promise.all`**

In `AppointmentFormContent.tsx`, add a new piece of state near the existing `patients`/`doctors`
state (`:32-33`):

```tsx
const [optionsLoaded, setOptionsLoaded] = useState(false);
```

Replace the fetch effect (`:44-47`):

```tsx
useEffect(() => {
  getPatients({ pageSize: 100 }).then((r) => setPatients(r.items)).catch(() => {});
  getDoctors({ pageSize: 100 }).then((r) => setDoctors(r.items)).catch(() => {});
}, []);
```

with:

```tsx
useEffect(() => {
  Promise.all([
    getPatients({ pageSize: 100 }).then((r) => setPatients(r.items)).catch(() => {}),
    getDoctors({ pageSize: 100 }).then((r) => setDoctors(r.items)).catch(() => {}),
  ]).finally(() => setOptionsLoaded(true));
}, []);
```

- [ ] **Step 2: Gate the reset effect on `optionsLoaded`**

Replace the reset effect (`:49-61`):

```tsx
useEffect(() => {
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

with:

```tsx
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

- [ ] **Step 3: Manually verify via the `run` skill**

- Edit an existing appointment (as Admin/Staff): Patient and Doctor dropdowns show the correct
  pre-selected names (disabled, but visibly correct) instead of "Select...".
- View an appointment detail (as Doctor): same — Patient and Doctor show correctly, both disabled.
- Create a new appointment (no existing `appointment`): `startsAt`/`durationMinutes` still populate
  correctly (from `defaultStartsAt` or the 30-minute default) — confirms the `optionsLoaded` gate
  didn't break the create path, just adds a sub-second delay before defaults appear.
- Click a calendar date to prefill "New Appointment": `defaultStartsAt` still carries through
  correctly once options load.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/appointments/AppointmentFormContent.tsx
git commit -m "fix: load patient/doctor dropdowns before resetting the appointment form"
```

- [ ] **Step 5: Open PR**

```bash
gh pr create --title "fix: load patient/doctor dropdowns before resetting the appointment form" \
  --body "Closes #33

The form reset (setting patientId/doctorId to the edited appointment's values) always ran before the async patients/doctors fetch could resolve, so the native <select> elements had no matching <option> yet — the dropdowns showed \"Select...\" instead of the actual patient/doctor, every time. Gates the reset behind an \`optionsLoaded\` flag set once both fetches settle.

Spec: \`docs/superpowers/specs/2026-08-13-appointment-status-and-edit-dropdowns.md\`" \
  --label bug --assignee willianbrecher
```

---

### Task 3: Surface the real error message on status-update failure (#32, frontend)

**Branch:** `fix/appointment-status-error-message` → PR `Refs #32`. Branch from `master` after
Task 2's PR merges.

**Files:**
- Modify: `frontend/src/features/appointments/AppointmentFormContent.tsx`

**Interfaces:** none — reuses the existing `getApiErrorMessage` helper (`frontend/src/lib/apiError.ts`), already used the same way in `DoctorFormContent.tsx:28` and `UsersPage.tsx:32,37,48`.

- [ ] **Step 1: Import `getApiErrorMessage`**

Add near the other imports:

```tsx
import { getApiErrorMessage } from "@/lib/apiError";
```

- [ ] **Step 2: Use it in `onStatusSubmit`'s catch block**

Replace (`:83-92`):

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

with:

```tsx
const onStatusSubmit = async (data: StatusFormData) => {
  try {
    await updateAppointmentStatus(appointment!.id, data.status as AppointmentStatus);
    toast.success("Status updated.");
    onSaved();
    onClose();
  } catch (err) {
    toast.error(getApiErrorMessage(err, "Failed to update status."));
  }
};
```

- [ ] **Step 3: Manually verify via the `run` skill**

- Trigger a genuinely invalid transition (e.g. try to change a `Completed` appointment's status,
  if reachable in the UI, or a transition combination still outside the allow-list) — toast now
  shows the backend's actual message (e.g. "Cannot transition from Completed to Scheduled.")
  instead of the generic "Failed to update status."
- A successful status update still shows "Status updated." (success path unaffected).

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/appointments/AppointmentFormContent.tsx
git commit -m "fix: surface the real error message when an appointment status update fails"
```

- [ ] **Step 5: Open PR**

```bash
gh pr create --title "fix: surface the real error message when an appointment status update fails" \
  --body "Refs #32

Replaces the generic \"Failed to update status.\" toast with the backend's actual message (e.g. which transition was rejected and why), using the existing \`getApiErrorMessage\` helper already used the same way elsewhere in the app.

Spec: \`docs/superpowers/specs/2026-08-13-appointment-status-and-edit-dropdowns.md\`" \
  --label bug --assignee willianbrecher
```

**After this PR merges, close #32 manually** (both its PRs — Task 1 and Task 3 — will have
landed).
