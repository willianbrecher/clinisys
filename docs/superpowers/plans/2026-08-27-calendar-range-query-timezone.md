# Calendar Range Query Timezone Mismatch Implementation Plan

> Recorded retroactively — the fix already shipped. Steps reflect what was done.
> Spec: `docs/superpowers/specs/2026-08-27-calendar-range-query-timezone.md`.

**Goal:** Send naive local time (no UTC offset) for the appointments calendar range query so it
matches the naive-wall-clock convention the create path already uses (#54). Frontend-only.

**Tech Stack:** React 18 / TypeScript, FullCalendar.

## Global Constraints

- Branch `fix/54-calendar-empty-column-timezone` referencing #54.
- Frontend-only, single PR → `Closes #54` (PR #55).

---

### Task 1: Naive-local range params in `handleCalendarEvents` (#54)

**Branch:** `fix/54-calendar-empty-column-timezone` → PR #55 `Closes #54`

**Files:** `frontend/src/features/appointments/AppointmentsPage.tsx`

- [x] **Step 1:** Add a `toNaiveLocalIso(date: Date): string` helper producing
  `YYYY-MM-DDTHH:mm:ss` (zero-padded, no offset), with a comment explaining the write/read
  convention mismatch it exists to bridge.
- [x] **Step 2:** In `handleCalendarEvents`, call
  `loadCalendar(toNaiveLocalIso(info.start), toNaiveLocalIso(info.end))` instead of
  `loadCalendar(info.startStr, info.endStr)`.
- [x] **Step 3:** Verify — in a browser whose TZ differs from the server's, a week whose first or
  last day is fully booked with morning appointments now renders every event in that boundary
  column (previously empty).
- [x] **Step 4:** Commit `fix: send naive local time for calendar range queries`, open PR #55,
  close #54.
