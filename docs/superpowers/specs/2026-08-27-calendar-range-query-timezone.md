# CliniSys — Calendar Range Query Timezone Mismatch Spec

Date: 2026-08-27
Status: Shipped — PR [#55](https://github.com/willianbrecher/clinisys/pull/55), merged 2026-08-27.
Recorded retroactively — the fix shipped straight from the issue without a spec/plan.
Issue: [#54](https://github.com/willianbrecher/clinisys/issues/54)

## 1. Goal

Stop the weekly/daily appointments calendar from rendering a boundary day column (first or last
visible day of the range) with zero events when appointments for that day actually exist.

## 2. Root cause — confirmed

Two date formats reach `GET /api/appointments?startDate=&endDate=` and are parsed inconsistently:

- **Appointment creation** (`AppointmentFormContent.tsx`) uses `<input type="datetime-local">`,
  producing a **naive** string with no UTC offset (`2026-08-24T09:00`). The backend parses it as
  `DateTimeKind.Unspecified`; `AppDbContext.cs`'s value converter relabels it `Utc` **without
  shifting the wall-clock value** — so `Appointment.StartsAt` is stored as literally the digits
  the user typed, tagged UTC by convention.
- **Calendar range fetch** (`AppointmentsPage.tsx`) passed FullCalendar's
  `info.startStr`/`info.endStr` straight through — under `timeZone: "local"` these **carry a UTC
  offset** (`2026-08-24T00:00:00-03:00`). `[FromQuery] DateTime? startDate` binds that via
  `DateTime.Parse`, which converts against the **server's** timezone and sets `Kind = Local`. In
  production (server TZ = UTC, browser TZ ≠ UTC) that's a real multi-hour shift, then the
  "relabel as Utc, don't shift" converter is applied on top of an already-shifted value.

Net: the query window is offset by the client's UTC offset relative to how `StartsAt` is stored.
Mid-week days absorb it; a boundary column with appointments clustered near the window edge can
have every event pushed outside the range → empty column.

## 3. What shipped (PR #55, frontend-only)

`AppointmentsPage.tsx` — a `toNaiveLocalIso(date: Date)` helper formatting `YYYY-MM-DDTHH:mm:ss`
with no offset, applied to FullCalendar's `info.start`/`info.end` (`Date` objects) instead of
passing `info.startStr`/`info.endStr`:

```tsx
loadCalendar(toNaiveLocalIso(info.start), toNaiveLocalIso(info.end)).then(/* … */)
```

The read path now uses the same naive-wall-clock convention as the write path, so both agree on
what "the time shown in the browser" means. The `why` is captured in a comment above the helper.

## 4. Non-goals

- **No backend change.** The backend already parses naive datetime strings correctly for
  appointment creation; only the calendar fetch needed to send the same shape.
- No change to the `AppDbContext` value converter or the "store naive, tag Utc" convention — that
  convention is load-bearing for the `datetime-local` create path and stays.
- No move to real timezone-aware storage (`timestamptz` + explicit zone) — a larger design
  question left untouched.

## 5. Related

Same "naive vs offset datetime" family as the
[appointment scheduling guardrails](2026-08-15-appointment-scheduling-guardrails.md) and
[calendar/date UI language](2026-08-15-calendar-date-ui-language.md) work.
