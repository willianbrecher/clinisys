# Date/Time Input Picker Click Implementation Plan

> Implement task-by-task, in order. Steps use checkbox (`- [ ]`) syntax for tracking. Each task ends with its own commit, per the branch/PR split defined below.

**Goal:** Clicking anywhere in a date/time input opens its native picker, not just the icon (#12). Spec: `docs/superpowers/specs/2026-08-10-datetime-input-picker-click.md`.

**Tech Stack:** React 18, TypeScript, Shadcn/UI `Input` component (frontend only — no backend changes).

## Global Constraints

- Follow root `CLAUDE.md`: one branch (`fix/<slug>`), referencing its issue.
- Single-layer (frontend-only) fix → PR uses `Closes #12`.
- Fix goes in the shared `Input` component, not at each of the 4 call sites — see spec §3 for why.
- No new locale strings, no visual changes — no locale-file or styling updates needed.

---

### Task 1: Open picker on click anywhere in date/time inputs (#12)

**Branch:** `fix/datetime-input-picker-click` → PR `Closes #12`

**Files:**
- Modify: `frontend/src/components/ui/input.tsx`

**Interfaces:** none — self-contained change to an existing shared component; no new exports.

- [ ] **Step 1: Add the picker-open click handler in `frontend/src/components/ui/input.tsx`**

```tsx
import * as React from "react"

import { cn } from "@/lib/utils"

const PICKER_TYPES = new Set(["date", "time", "datetime-local"]);

const Input = React.forwardRef<HTMLInputElement, React.ComponentProps<"input">>(
  ({ className, type, onClick, ...props }, ref) => {
    return (
      <input
        type={type}
        onClick={(e) => {
          onClick?.(e);
          if (type && PICKER_TYPES.has(type) && !e.currentTarget.disabled) {
            e.currentTarget.showPicker?.();
          }
        }}
        className={cn(
          "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-base ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 md:text-sm",
          className
        )}
        ref={ref}
        {...props}
      />
    )
  }
)
Input.displayName = "Input"

export { Input }
```

Only the `onClick` prop destructuring and handler are new; `className`, `ref`, and the rest of the
component are unchanged.

- [ ] **Step 2: Manually verify via the `run` skill**

Check all 4 affected fields, clicking in the middle of the field text (not the icon) each time:
- Patient form → date of birth (`PatientFormContent.tsx`) — picker should open.
- Settings page → clinic open time and close time (`SettingsPage.tsx`, 2 fields) — picker should open.
- Appointment form → start date/time (`AppointmentFormContent.tsx`) — picker should open when
  creating/editing; when viewing an existing appointment in detail/read-only mode (field
  `disabled`), clicking should **not** throw a console error and should behave as before
  (no picker, since the field is disabled).

Also confirm normal typing/tabbing into these fields still works, and that other input types
(`text`, `number`, `email`, etc. — e.g. patient name, appointment duration) are unaffected.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/ui/input.tsx
git commit -m "fix: open date/time picker on click anywhere in the field"
```

- [ ] **Step 4: Open PR**

```bash
gh pr create --title "fix: open date/time picker on click anywhere in the field" \
  --body "Closes #12

Clicking anywhere in a date/time input now opens the native picker (via \`showPicker()\`), not just the icon. Centralized in the shared \`Input\` component since all 4 date/time fields in the app (patient DOB, clinic open/close time, appointment start) already go through it." \
  --label feature --assignee willianbrecher
```

Note: this is a single-layer, single-PR fix, so `Closes #12` applies (per root `CLAUDE.md`, `Refs`
is only for issues split across multiple PRs). Swap `--label feature` for `--label bug` at
creation time if that fits better.
