# Appointment Scheduling Guardrails Implementation Plan

> Implement task-by-task, in order. Steps use checkbox (`- [ ]`) syntax for tracking. **Merge each
> PR to `master` before starting the next task** — tasks 2 and 3 both touch `AppointmentsPage.tsx`
> and `AppointmentFormContent.tsx`, and task 3 extends logic task 2 introduces, so branching each
> off the latest `master` avoids conflicts and keeps each diff scoped to its own issue.

**Goal:** Close #32 (already resolved), then stop the calendar and appointment form from allowing
past-date (#31) and outside-open-hours (#38) selections client-side, instead of only failing after
a round trip to the backend.
Spec: `docs/superpowers/specs/2026-08-15-appointment-scheduling-guardrails.md`.

**Tech Stack:** React 18/TypeScript/React Hook Form/Yup/FullCalendar (frontend only — the backend
already enforces both rules; no backend changes in this plan).

## Global Constraints

- Follow root `CLAUDE.md`: one branch (`fix/<slug>`) per PR, referencing its issue.
- #31 and #38 are both frontend-only → each PR uses `Closes #N`.
- Both issues are `bug`-labeled — use `bug` on both PRs.
- Implementation order: **Task 1 (close #32) → Task 2 (#31) → Task 3 (#38)**. Task 3 extends the
  `dateClick` guard and Yup schema Task 2 introduces (open-hours checks layered on top of the
  past-date checks) — doing #31 first means #38 adds to already-reviewed code instead of the two
  rules landing tangled in one diff.

---

### Task 1: Close #32 (already resolved, no code change)

Both of #32's PRs are merged: `fix/appointment-status-transitions-backend` (#34, backend transition
table) and `fix/appointment-status-error-message` (#36, frontend error surfacing) — see
`docs/superpowers/plans/2026-08-13-appointment-status-and-edit-dropdowns.md`'s closing note. Close
the issue manually now, since neither merged PR used `Closes #32` (both correctly used `Refs #32`,
per that plan's constraint that no single PR should auto-close a multi-PR issue).

- [ ] **Step 1: Close #32**

```bash
gh issue close 32 --comment "Both PRs for this issue are merged: #34 (backend transition table — allows no-op and Scheduled→Completed) and #36 (frontend surfaces the real backend error message on a failed status update). Closing manually per the original plan's note."
```

---

### Task 2: Block past dates/times (#31)

**Branch:** `fix/appointment-block-past-dates` → PR `Closes #31`

**Files:**
- Modify: `frontend/src/features/appointments/AppointmentsPage.tsx`
- Modify: `frontend/src/features/appointments/AppointmentFormContent.tsx`
- Modify: `frontend/src/features/appointments/appointment.schema.ts`

**Interfaces:** `appointmentSchema` (named export) becomes `buildAppointmentSchema()` (a factory
function) — needed starting Task 3, where the schema must validate against dynamically-fetched
clinic settings, so introducing the factory shape now (even though Task 2 doesn't yet need
parameters) avoids a second signature change next task. `AppointmentFormData`'s type derivation
moves from `yup.InferType<typeof appointmentSchema>` to
`yup.InferType<ReturnType<typeof buildAppointmentSchema>>`.

- [ ] **Step 1: Turn `appointmentSchema` into a factory in `appointment.schema.ts`**

Replace:

```ts
export const appointmentSchema = yup.object({
  patientId: yup.string().uuid().required("Patient is required"),
  doctorId: yup.string().uuid().required("Doctor is required"),
  startsAt: yup.string().required("Start date/time is required"),
  durationMinutes: yup.number().min(5).max(480).required("Duration is required"),
  notes: yup.string().optional(),
});
```

with:

```ts
export function buildAppointmentSchema() {
  return yup.object({
    patientId: yup.string().uuid().required("Patient is required"),
    doctorId: yup.string().uuid().required("Doctor is required"),
    startsAt: yup.string().required("Start date/time is required")
      .test("future", "Start date/time must be in the future",
        (value) => !value || new Date(value) > new Date()),
    durationMinutes: yup.number().min(5).max(480).required("Duration is required"),
    notes: yup.string().optional(),
  });
}
```

Update the type export:

```ts
export type AppointmentFormData = yup.InferType<ReturnType<typeof buildAppointmentSchema>>;
```

- [ ] **Step 2: Use the factory in `AppointmentFormContent.tsx` and add `min` to the input**

Update the import and resolver wiring:

```tsx
import { buildAppointmentSchema, statusSchema, type AppointmentFormData, type StatusFormData } from "./appointment.schema";
```

```tsx
const appointmentSchema = useMemo(() => buildAppointmentSchema(), []);

const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<AppointmentFormData>({
  resolver: yupResolver(appointmentSchema) as unknown as Resolver<AppointmentFormData>,
});
```

(Add `useMemo` to the existing `import { useEffect, useState } from "react";` line.)

Add a `minStartsAt` computed value near the top of the component body (local wall-clock "now,"
formatted the same way `appointment.startsAt.slice(0, 16)` already is elsewhere in this file —
**not** `toISOString()`, which would read as tomorrow's date for anyone west of UTC in the evening):

```tsx
const pad = (n: number) => String(n).padStart(2, "0");
const now = new Date();
const minStartsAt = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`;
```

Apply it to the input (`:142`):

```tsx
<Input type="datetime-local" min={minStartsAt} {...register("startsAt")} disabled={isDetail} readOnly={isDetail} />
```

- [ ] **Step 3: Add `validRange` and a past-instant guard to the calendar**

In `AppointmentsPage.tsx`, add a local-date `todayStr` helper (same UTC-offset caveat as above —
`toISOString()` would be wrong near midnight):

```tsx
const pad = (n: number) => String(n).padStart(2, "0");
const now = new Date();
const todayStr = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;

const isPastClick = (date: Date, allDay: boolean) => {
  if (allDay) {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return date < today;
  }
  return date < new Date();
};
```

Replace the `dateClick` handler and add `validRange` (`:190-197`):

```tsx
validRange={{ start: todayStr }}
slotMinTime={slotMinTime}
slotMaxTime={slotMaxTime}
hiddenDays={[0, 1, 2, 3, 4, 5, 6].filter((d) => !openDays.includes(d))}
selectable
selectConstraint={{ daysOfWeek: openDays, startTime: slotMinTime, endTime: slotMaxTime }}
dateClick={(info: { dateStr: string; date: Date; allDay: boolean }) => {
  if (isPastClick(info.date, info.allDay)) return;
  navigate("/appointments/new", { state: { defaultStartsAt: info.dateStr } });
}}
```

`validRange` handles day-granularity blocking (past weeks/months become non-navigable/non-clickable)
across all views; `isPastClick` additionally covers the same-day, past-hour case in
`timeGridWeek`/`timeGridDay` that `validRange` can't express. This step deliberately does **not**
touch open-hours/day logic — that's Task 3, even though `openDays`/`slotMinTime`/`slotMaxTime` are
already in scope here; keeping this guard past-only keeps this diff scoped to #31.

- [ ] **Step 4: Manually verify via the `run` skill**

- Calendar: navigating to a past week/month — cells render disabled/grayed, `dateClick` doesn't
  fire (no navigation to "New Appointment").
- Calendar: in `timeGridWeek`/`timeGridDay` on *today*, a slot earlier than the current time does
  not navigate on click; a slot later than now does.
- Form: opening "+ New Appointment" — the `startsAt` input's picker doesn't allow selecting a time
  before "now" (native `min` enforcement); attempting to bypass and submit a past value some other
  way shows the inline "Start date/time must be in the future" error instead of submitting.
- Form: a valid future `startsAt` still submits successfully (creates/reschedules normally) —
  confirms the `future` Yup test doesn't false-positive on legitimate values.
- Reschedule: editing an existing (possibly already-past) appointment still opens correctly
  (`isDetail`/status-update paths are untouched); attempting to reschedule it *to* a past time is
  blocked the same way as create.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/appointments/appointment.schema.ts frontend/src/features/appointments/AppointmentFormContent.tsx frontend/src/features/appointments/AppointmentsPage.tsx
git commit -m "fix: block scheduling appointments in the past (calendar and form)"
```

- [ ] **Step 6: Open PR**

```bash
gh pr create --title "fix: block scheduling appointments in the past (calendar and form)" \
  --body "Closes #31

Neither the calendar nor the appointment form stopped a user from picking a past date/time — the backend already rejected it, but only after a round trip. Adds \`validRange\` + a \`dateClick\` guard to the calendar (blocking past days across all views, and past hours today in the time-grid views), and a \`min\` attribute + Yup future-date check to the form's \`startsAt\` input, so the invalid selection is prevented up front instead of surfacing as a generic failed-submit toast.

Spec: \`docs/superpowers/specs/2026-08-15-appointment-scheduling-guardrails.md\`" \
  --label bug --assignee willianbrecher
```

---

### Task 3: Block outside-open-hours dates/times (#38)

**Branch:** `fix/appointment-block-outside-open-hours` → PR `Closes #38`. Branch from `master`
after Task 2's PR merges.

**Files:**
- Add: `frontend/src/features/appointments/clinicHours.ts`
- Modify: `frontend/src/features/appointments/appointment.schema.ts`
- Modify: `frontend/src/features/appointments/AppointmentFormContent.tsx`
- Modify: `frontend/src/features/appointments/AppointmentsPage.tsx`

**Interfaces:** new `clinicHours.ts` module exporting `isPastInstant`, `deriveOpenDays`, and
`isWithinOpenHours` — small pure helpers shared between the calendar's `dateClick` guard and the
form's Yup schema, which both need the identical open-hours-window check
(day-of-week ∈ `openDays`, and `[startTime, startTime + durationMinutes]` ⊆ `[openTime, closeTime]`
same calendar day). Centralizing this avoids the two call sites drifting out of sync with each
other (or with the backend's `ValidateOpenHours`, which this mirrors).

- [ ] **Step 1: Add `clinicHours.ts`**

Create `frontend/src/features/appointments/clinicHours.ts`:

```ts
import type { ClinicSettingsModel } from "@/api/types";

export function deriveOpenDays(settings: ClinicSettingsModel | null): number[] {
  return settings?.openDays ? settings.openDays.split(",").map(Number) : [1, 2, 3, 4, 5];
}

/** True if `startsAt` (a "YYYY-MM-DDTHH:mm" local string) plus `durationMinutes` fits inside
 * `settings`'s open days/hours for that calendar day. Mirrors the backend's ValidateOpenHours. */
export function isWithinOpenHours(
  startsAt: string, durationMinutes: number, settings: ClinicSettingsModel | null,
): boolean {
  if (!settings || !startsAt) return true;
  const start = new Date(startsAt);
  if (!deriveOpenDays(settings).includes(start.getDay())) return false;

  const end = new Date(start.getTime() + durationMinutes * 60000);
  if (end.getDate() !== start.getDate()) return false; // rolled past midnight

  const pad = (n: number) => String(n).padStart(2, "0");
  const timeOf = (d: Date) => `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  return timeOf(start) >= settings.openTime && timeOf(end) <= settings.closeTime;
}
```

(`isPastInstant`/day-granularity past-check from Task 2 stays local to each file — it's a one-line
comparison, not worth centralizing.)

- [ ] **Step 2: Extend `buildAppointmentSchema` to accept clinic settings**

In `appointment.schema.ts`, import the new helper and change the factory's signature:

```ts
import { isWithinOpenHours } from "./clinicHours";
import type { ClinicSettingsModel } from "@/api/types";

export function buildAppointmentSchema(clinicSettings: ClinicSettingsModel | null) {
  return yup.object({
    patientId: yup.string().uuid().required("Patient is required"),
    doctorId: yup.string().uuid().required("Doctor is required"),
    startsAt: yup.string().required("Start date/time is required")
      .test("future", "Start date/time must be in the future",
        (value) => !value || new Date(value) > new Date())
      .test("open-hours", "Selected time is outside clinic open hours", function (value) {
        if (!value) return true;
        return isWithinOpenHours(value, this.parent.durationMinutes ?? 0, clinicSettings);
      }),
    durationMinutes: yup.number().min(5).max(480).required("Duration is required"),
    notes: yup.string().optional(),
  });
}
```

- [ ] **Step 3: Fetch clinic settings in the form and wire the schema + `max`**

In `AppointmentFormContent.tsx`:

1. Add imports: `getClinicSettings` from `@/api/clinicSettings`, `ClinicSettingsModel` from
   `@/api/types` (add to the existing type-only import line).
2. Add state and a fetch effect, alongside the existing `patients`/`doctors` fetch:

```tsx
const [clinicSettings, setClinicSettings] = useState<ClinicSettingsModel | null>(null);

useEffect(() => {
  getClinicSettings().then(setClinicSettings).catch(() => {});
}, []);
```

3. Update the schema `useMemo` from Task 2 to depend on the fetched settings:

```tsx
const appointmentSchema = useMemo(() => buildAppointmentSchema(clinicSettings), [clinicSettings]);
```

4. Add `max`, tracking the selected date's close time — needs `watch` from `useForm`:

```tsx
const { register, handleSubmit, reset, watch, formState: { errors, isSubmitting } } = useForm<AppointmentFormData>({
  resolver: yupResolver(appointmentSchema) as unknown as Resolver<AppointmentFormData>,
});
```

```tsx
const startsAtDate = watch("startsAt")?.slice(0, 10);
const maxStartsAt = startsAtDate && clinicSettings ? `${startsAtDate}T${clinicSettings.closeTime}` : undefined;
```

```tsx
<Input type="datetime-local" min={minStartsAt} max={maxStartsAt} {...register("startsAt")} disabled={isDetail} readOnly={isDetail} />
```

- [ ] **Step 4: Extend the calendar's `dateClick` guard and add `businessHours`**

In `AppointmentsPage.tsx`, import `deriveOpenDays`/`isWithinOpenHours` from `./clinicHours` and
replace the local `openDays` derivation (`:89-91`) with `deriveOpenDays(settings)`. Add
`businessHours` (visual shading in every view, including `dayGridMonth`, where
`hiddenDays`/`slotMinTime`/`slotMaxTime` don't apply) and extend the click guard:

```tsx
businessHours={{ daysOfWeek: openDays, startTime: slotMinTime, endTime: slotMaxTime }}
```

```tsx
dateClick={(info: { dateStr: string; date: Date; allDay: boolean }) => {
  if (isPastClick(info.date, info.allDay)) return;
  if (!info.allDay && !isWithinOpenHours(info.dateStr.slice(0, 16), 0, settings)) return;
  if (info.allDay && !openDays.includes(info.date.getDay())) return;
  navigate("/appointments/new", { state: { defaultStartsAt: info.dateStr } });
}}
```

(`durationMinutes: 0` in the `isWithinOpenHours` call is intentional — the calendar click only
proposes a *start* instant; the real duration-fits-before-close check happens once the user has
actually chosen a duration, in the form.)

- [ ] **Step 5: Surface the real error if a rule is bypassed client-side**

Client-side validation can't cover every case (e.g. clinic hours changed in another tab while the
form was open) — the backend remains authoritative. Replace `onSubmit`'s bare
`catch { toast.error("Failed to save appointment."); }` (`AppointmentFormContent.tsx:83-85`) with:

```tsx
} catch (err) {
  toast.error(getApiErrorMessage(err, "Failed to save appointment."));
}
```

`getApiErrorMessage` is already imported in this file (used by `onStatusSubmit`, fixed in #32).

- [ ] **Step 6: Manually verify via the `run` skill**

- Calendar: `dayGridMonth` view now visually shades non-open days (via `businessHours`); clicking a
  closed day doesn't navigate.
- Calendar: `timeGridWeek`/`timeGridDay` — unchanged from today's `hiddenDays`/`slotMinTime`/
  `slotMaxTime` behavior (those slots were already un-renderable), confirms Task 3 doesn't regress
  Task 2 or pre-existing calendar behavior.
- Form: pick a date the clinic is closed on (e.g. a Sunday, if not in `openDays`) — inline error
  "Selected time is outside clinic open hours" on submit attempt.
- Form: pick an open day, but a `startsAt` time before `openTime` or a `durationMinutes` that pushes
  past `closeTime` — same inline error; the native `max` also prevents picking a time past
  `closeTime` directly in the picker for that date.
- Form: a fully valid open-hours, future `startsAt` still submits successfully.
- Confirm `GET /api/clinic-settings` (`AllowAnonymous`) is reachable from the form for all three
  roles (Admin/Staff/Doctor) that can open this form — no new auth gap introduced.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/features/appointments/clinicHours.ts frontend/src/features/appointments/appointment.schema.ts frontend/src/features/appointments/AppointmentFormContent.tsx frontend/src/features/appointments/AppointmentsPage.tsx
git commit -m "fix: block scheduling appointments outside clinic open hours (calendar and form)"
```

- [ ] **Step 8: Open PR**

```bash
gh pr create --title "fix: block scheduling appointments outside clinic open hours (calendar and form)" \
  --body "Closes #38

Neither the calendar (outside its two time-grid views) nor the appointment form stopped a user from picking a day/time the clinic isn't open — the backend already rejected it, but only after a round trip. Adds a shared \`clinicHours.ts\` helper (mirroring the backend's \`ValidateOpenHours\`), a \`businessHours\` prop + extended \`dateClick\` guard on the calendar, and a dynamic \`max\` + Yup open-hours check on the form's \`startsAt\` input — built on top of #31's past-date guard from the same PR sequence.

Spec: \`docs/superpowers/specs/2026-08-15-appointment-scheduling-guardrails.md\`" \
  --label bug --assignee willianbrecher
```

**After this PR merges**, both #31 and #38 are fully addressed client-side, with the backend's
existing `ValidateOpenHours`/future-date validators remaining as the authoritative safety net for
anything that bypasses the UI (direct API calls, or a clinic-hours change made in another tab while
the form is still open).
