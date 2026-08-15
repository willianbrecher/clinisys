# CliniSys — Appointment Scheduling Guardrails Spec

Date: 2026-08-15
Status: Draft
Issues: [#31](https://github.com/willianbrecher/clinisys/issues/31),
[#38](https://github.com/willianbrecher/clinisys/issues/38) — plus disposition of
[#32](https://github.com/willianbrecher/clinisys/issues/32), see §4.

## 1. Goal

Nothing on the frontend currently stops a user from picking a past date/time or an out-of-open-hours
date/time for an appointment — the backend already rejects both, but only after a round trip, and
only via a generic toast once the request fails. #31 and #38 are two independent business rules
(recency vs. open-hours window) enforced identically today (backend-only) and requested identically
(client-side prevention at the same two entry points: the calendar's `dateClick`, and the form's
`startsAt` input). This spec covers both together since implementing them separately would mean
touching the exact same lines twice.

## 2. Current behavior — confirmed

### Backend (already correct, no changes needed)

Both rules are already enforced server-side, identically for create and reschedule:

- **Future-only**: `CreateAppointmentCommandValidator.cs:13` and
  `RescheduleAppointmentCommandValidator.cs:11` both do
  `RuleFor(x => x.StartsAt).GreaterThan(DateTime.UtcNow).WithMessage("StartsAt must be in the future.")`.
- **Open hours**: `CreateAppointmentCommandHandler.ValidateOpenHours` (`:47-58`) and the equivalent
  inline block in `RescheduleAppointmentCommandHandler.Handle` (`:36-45`) both check the appointment's
  day-of-week against `ClinicSettings.OpenDays`, and its time range against `OpenTime`/`CloseTime`,
  throwing `ConflictException("The clinic is not open on that day.")` /
  `ConflictException("The appointment falls outside clinic open hours.")`.

This is single-source-of-truth business logic living entirely in `CliniSys.Application`; the frontend
work below is purely additive UI prevention, not a change to what's actually allowed.

### Frontend gaps

**Calendar** — `frontend/src/features/appointments/AppointmentsPage.tsx`:
- Already fetches `ClinicSettingsModel` (`:40-42`) and derives `openDays`/`slotMinTime`/`slotMaxTime`
  (`:89-94`), applied as `slotMinTime`/`slotMaxTime`/`hiddenDays`/`selectConstraint` (`:190-194`).
  This constrains the *rendered* slots in `timeGridWeek`/`timeGridDay` to open hours/days, but:
  - `dayGridMonth` has no time granularity, so a day-cell click there produces a date with no
    open-hours check applied to it.
  - `selectConstraint` only governs drag-*select*; no `select` handler is registered, only
    `dateClick` (`:195-197`), so `selectConstraint` is dead configuration today.
  - No `validRange` is set, so past dates/weeks/months are fully navigable and clickable.
  - `dateClick` unconditionally does
    `navigate("/appointments/new", { state: { defaultStartsAt: info.dateStr } })` — whatever was
    clicked passes straight through into the form, past or closed alike.

**Form** — `frontend/src/features/appointments/AppointmentFormContent.tsx` +
`appointment.schema.ts`:
- `startsAt` is a bare `<input type="datetime-local">` (`:142`) with no `min`/`max`.
- The Yup schema (`appointment.schema.ts:6-7`) only requires a non-empty string — no future-date
  check, no open-hours check, no check that `startsAt + durationMinutes` stays within `closeTime`.
- The form never fetches `ClinicSettingsModel` at all today — only `AppointmentsPage` does.

Because of this, any invalid `startsAt` reaches the backend and fails with a generic
`"Failed to save appointment."` toast (`AppointmentFormContent.tsx` `onSubmit`'s bare
`catch { toast.error(...) }`, `:83-85`) — the specific backend message (e.g. "The doctor already has
an appointment at that time.") never surfaces, same class of bug already fixed for
`onStatusSubmit` in #32's Task 3.

## 3. Proposed fix

Two frontend-only rules, both computed from clinic settings + "now," applied at both entry points.
No new dependency — reuses `getClinicSettings()` (already used by `AppointmentsPage`) and the
existing `getApiErrorMessage` helper (already used by `onStatusSubmit`).

### 3.1 Shared: fetch clinic settings in the form

`AppointmentFormContent.tsx` gains its own `useEffect` calling `getClinicSettings()` into local
state, independent of `AppointmentsPage`'s copy (the form is rendered as a nested route/dialog and
doesn't share state with the page — mirrors how `patients`/`doctors` are already fetched
independently in this same file).

### 3.2 #31 — block past dates/times

**Calendar**: add `validRange={{ start: <today's date, YYYY-MM-DD> }}` to `<FullCalendar>` — this
disables navigation/rendering of past days across all views (month, week, day), which covers the
day-granularity case. For the same-day, past-hour case in `timeGridWeek`/`timeGridDay` (where
`validRange` can't express sub-day precision), add a guard in `dateClick`: parse `info.date`
(FullCalendar's `Date` object, not just the string) and no-op (rather than navigate) if it's before
`new Date()`.

**Form**: compute `minStartsAt` once (local "now" formatted as `YYYY-MM-DDTHH:mm`, matching the
existing `.slice(0, 16)` convention used for `appointment.startsAt`/`defaultStartsAt` elsewhere in
this file) and set it as the input's `min`. Add a Yup `.test()` on `startsAt` rejecting values not
strictly after "now," with a clear inline message — reusing the existing
`errors.startsAt && <p>...</p>` rendering already in place for this field (`:143`), no new UI needed.

### 3.3 #38 — block outside clinic open hours

**Calendar**: add a `businessHours` prop derived from `openDays`/`openTime`/`closeTime` (visual
shading in every view, including `dayGridMonth`, where `hiddenDays`/`slotMinTime`/`slotMaxTime`
don't apply). Extend the same `dateClick` guard added for 3.2 to also reject clicks whose
day-of-week isn't in `openDays`, or (for time-grid clicks) whose time falls before `openTime`/after
`closeTime`.

**Form**: set the input's `max` from `closeTime` for whatever date is currently entered (recomputed
on `startsAt`'s date portion changing — a `watch("startsAt")`-driven derived value, since `max` needs
to track the selected date, unlike `min` which is static for the form's lifetime). Extend the Yup
schema with a second `.test()` cross-checking `startsAt`'s day-of-week against `openDays`, its time
against `openTime`/`closeTime`, and — using the sibling `durationMinutes` field via Yup's
`test(..., function(value) { ...this.parent.durationMinutes... })` — that `startsAt + durationMinutes`
doesn't run past `closeTime`.

### 3.4 Complement: surface the real error if a rule is bypassed

Since client-side validation can't cover every case perfectly (e.g. clinic hours changing in another
tab while the form is open), replace `onSubmit`'s bare `catch { toast.error("Failed to save
appointment."); }` (`:83-85`) with `catch (err) { toast.error(getApiErrorMessage(err, "Failed to
save appointment.")); }` — identical fix to `onStatusSubmit`'s in #32. This isn't new scope; it's the
explicit "safety net" both #31 and #38 call for once the primary prevention is bypassed.

## 4. Disposition of #32

#32's actual bugs (incomplete status-transition table; generic error toast on status update) are
already fixed and merged — `fix/appointment-status-transitions-backend` (PR #34, backend transition
table) and `fix/appointment-status-error-message` (PR #36, frontend `getApiErrorMessage` on
`onStatusSubmit`) both merged into `master` on 2026-08-13. No further code change is needed for #32;
this spec's plan closes it manually as its first step, per the original
`2026-08-13-appointment-status-and-edit-dropdowns.md` plan's closing instruction, which was left
pending.

## 5. Non-goals

- No backend changes — both rules are already correctly enforced server-side; this spec is
  UI-prevention only.
- No change to `slotMinTime`/`slotMaxTime`/`hiddenDays`/`selectConstraint` — these stay as-is;
  `businessHours` and `validRange` are additive, not replacements.
- No generalized "clinic settings" React context/hook extraction — the form fetches its own copy
  independently, matching this file's existing pattern for `patients`/`doctors` rather than
  introducing shared state management as a side effect of this fix.
- No change to how `startsAt` is represented/sent (still a bare local wall-clock string, per the
  existing `Kind=Utc` stamping convention in `AppDbContext` — frontend "is this in the future"
  comparisons only need to be internally consistent with the browser's own `Date`, not perform any
  UTC conversion, since the backend stamps the same naive value without shifting it).
