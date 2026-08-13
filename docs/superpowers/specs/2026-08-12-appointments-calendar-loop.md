# CliniSys — Appointments Calendar Infinite Fetch/Render Loop Spec

Date: 2026-08-12
Status: Draft
Issue: [#23](https://github.com/willianbrecher/clinisys/issues/23)

## 1. Goal

Opening the appointments calendar view should fetch appointments for the visible date range once
and settle — no repeated fetching/re-rendering.

## 2. Current behavior — confirmed

Full relevant source: `frontend/src/features/appointments/AppointmentsPage.tsx`.

The issue's own "likely cause" is confirmed correct. The `FullCalendar` `events` prop
(lines 184-195) is an inline async arrow function defined directly in JSX:

```tsx
events={async (info, successCb) => {
  const items = await loadCalendar(info.startStr, info.endStr);
  setData(items);
  successCb(items.map((a) => ({
    id: a.id,
    title: `${a.patientName} (${a.doctorName})`,
    start: a.startsAt,
    end: new Date(new Date(a.startsAt).getTime() + a.durationMinutes * 60000).toISOString(),
    backgroundColor: STATUS_COLORS[a.status],
    borderColor: STATUS_COLORS[a.status],
  })));
}}
```

Being written inline, this function is a new reference on every render of `AppointmentsPage`.
`setData(items)` inside it updates the shared `data` state (`useState` at line 33), which
re-renders `AppointmentsPage`, which re-creates the inline `events` function, which `FullCalendar`
treats as a new/changed event source and refetches → `setData` fires again → loop, indefinitely.

`data` is shared between the list tab and the calendar tab today. The list tab populates it via
`loadList` (line 43-48, called only when `tab === "list"`, line 57). The calendar tab's own
`events` callback is currently the *only* thing that populates `data` while on the calendar tab —
`eventClick` (lines 180-183) depends on it:

```tsx
eventClick={(info) => {
  const appt = data.find((a) => a.id === info.event.id);
  if (appt) openAppointment(appt);
}}
```

Since the app opens on the calendar tab by default (`useState<Tab>("calendar")`, line 32) and
`loadList` never runs there, `data` would otherwise never be populated for `eventClick` to search
— that dependency is why `setData` was put inside the `events` callback in the first place, not
an oversight.

`loadCalendar` (lines 50-55) is already `useCallback`-memoized on `[role, doctorId]`, so it is
stable across re-renders unless the doctor filter actually changes — it isn't the source of the
loop.

## 3. Proposed fix

Two changes, both needed together:

**a) Stop routing calendar data through `data`/`setData`.** Attach the full `AppointmentModel` to
each FullCalendar event's `extendedProps` at map time, so `eventClick` can read it directly off
the clicked event instead of searching `data`:

```tsx
events={async (info, successCb) => {
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
}}
```

```tsx
eventClick={(info) => {
  const appt = info.event.extendedProps.appointment as AppointmentModel;
  openAppointment(appt);
}}
```

This removes the `setData` call from the events callback entirely — `data` becomes exclusively
the list tab's state, matching what its name already implied.

**b) Make `events` a stable reference.** Even without the `setData` call, an inline arrow function
is still re-created every render (any state change anywhere in `AppointmentsPage` — pagination,
tab switches — would re-create it). Wrap it in `useCallback`, memoized on `loadCalendar` (its only
dependency):

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

...and pass `events={handleCalendarEvents}` on the `FullCalendar` element. `FullCalendar` will
only refetch when `loadCalendar` itself changes (i.e. `role`/`doctorId` changes) or the visible
date range changes (native `FullCalendar` behavior, unaffected by this fix) or `calendarKey`
forces a remount after save (line 163, unchanged) — not on every render.

Both changes are required: (a) alone still loops because the function reference still changes
every render; (b) alone still loops because `setData` still forces a re-render that (without a
stable reference) would re-trigger the fetch — although with (b) applied, (a) becomes the only
remaining state-changing side effect inside the callback, so technically (b) alone would stop the
loop once `data` stops changing. Implementing both together removes the dependency instead of
leaving it fragile.

## 4. Non-goals

- No change to `loadList`/list-tab behavior — untouched.
- No change to `STATUS_COLORS`, calendar view options, or `calendarKey` remount-on-save mechanism.
- No change to the appointments API (`getAppointments`) or its params.
- Does not address styling/UX of the calendar — fetch/render correctness only.
