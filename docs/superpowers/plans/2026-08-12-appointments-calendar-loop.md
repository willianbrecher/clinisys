# Appointments Calendar Infinite Loop Fix Implementation Plan

> Implement task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop the appointments calendar from looping fetch/render on open (#23) — the calendar
should fetch the visible date range once and settle.
Spec: `docs/superpowers/specs/2026-08-12-appointments-calendar-loop.md`.

**Tech Stack:** React 18, TypeScript, `@fullcalendar/react` (frontend only — no backend changes).

## Global Constraints

- Follow root `CLAUDE.md`: branch `feature/<slug>` or `fix/<slug>` referencing issue #23. This is
  a bug fix → `fix/`.
- Single-layer (frontend-only) change → PR uses `Closes #23`.
- Repo has no `feature` label; this is a `bug` per the issue's own label — use `bug`.

---

### Task 1: Fix the calendar fetch/render loop (#23)

**Branch:** `fix/appointments-calendar-loop` → PR `Closes #23`

**Files:**
- Modify: `frontend/src/features/appointments/AppointmentsPage.tsx`

**Interfaces:** none — internal refactor of the `events`/`eventClick` handlers; `data` state stays
(now list-tab-only), no new props or exports.

- [ ] **Step 1: Add `extendedProps` carrying the full appointment to each mapped calendar event**

In the existing `events` inline handler (lines 184-195), add `extendedProps: { appointment: a }`
to each mapped event object, alongside `id`/`title`/`start`/`end`/`backgroundColor`/`borderColor`.

- [ ] **Step 2: Update `eventClick` to read from `extendedProps` instead of searching `data`**

Replace:

```tsx
eventClick={(info) => {
  const appt = data.find((a) => a.id === info.event.id);
  if (appt) openAppointment(appt);
}}
```

with:

```tsx
eventClick={(info) => {
  const appt = info.event.extendedProps.appointment as AppointmentModel;
  openAppointment(appt);
}}
```

(No more `if (appt)` guard needed — `extendedProps.appointment` is always set by the mapping in
Step 1, so it can't be `undefined` the way `data.find` could return one.)

- [ ] **Step 3: Extract the `events` handler out of JSX into a `useCallback`, drop `setData`**

Above the `return` statement, add (near `loadCalendar`, which it depends on):

```tsx
const handleCalendarEvents = useCallback(
  async (info: { startStr: string; endStr: string }, successCb: (events: unknown[]) => void) => {
    const items = await loadCalendar(info.startStr, info.endStr);
    successCb(items.map((a) => ({
      id: a.id,
      title: `${a.patientName} (${a.doctorName})`,
      start: a.startsAt,
      end: new Date(new Date(a.startsAt).getTime() + a.durationMinutes * 60000).toISOString(),
      backgroundColor: STATUS_COLORS[a.status],
      borderColor: STATUS_COLORS[a.status],
      extendedProps: { appointment: a },
    })));
  },
  [loadCalendar],
);
```

Remove the old inline `events={async (info, successCb) => { ... }}` prop entirely (including its
`setData(items)` call) and replace it with `events={handleCalendarEvents}` on the `FullCalendar`
element.

- [ ] **Step 4: Manually verify via the `run` skill**

- Open Appointments (default calendar tab). Confirm the network tab shows exactly **one**
  `GET /api/appointments` request for the initial visible range, not a repeating stream.
- Confirm appointments render on the calendar and stay rendered (no flicker/clearing loop).
- Click an event → confirms `eventClick` still opens the correct appointment (via
  `extendedProps`, not `data`).
- Navigate the calendar (next/prev week, switch to month/day view) → confirms a **new** single
  fetch fires per range change, then settles again (not a regression from memoizing `events`).
- Switch to List tab and back to Calendar tab → list tab still loads/paginates correctly
  (`data`/`loadList` untouched); calendar tab still fetches once on return.
- Create/edit an appointment and save → `calendarKey` bump still forces the calendar to remount
  and refetch the new data once (not looping).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/appointments/AppointmentsPage.tsx
git commit -m "fix: stop appointments calendar fetch/render loop"
```

- [ ] **Step 6: Open PR**

```bash
gh pr create --title "fix: stop appointments calendar fetch/render loop" \
  --body "Closes #23

The FullCalendar \`events\` prop was an inline async function that called \`setData\` on every fetch, creating a new function reference each render — FullCalendar treated that as a new event source and refetched, looping indefinitely. Moves the appointment payload into each event's \`extendedProps\` (so \`eventClick\` no longer needs the shared \`data\` state) and memoizes the \`events\` handler with \`useCallback\`, so it only refetches when the doctor filter, visible date range, or \`calendarKey\` actually change.

Spec: \`docs/superpowers/specs/2026-08-12-appointments-calendar-loop.md\`" \
  --label bug --assignee willianbrecher
```
