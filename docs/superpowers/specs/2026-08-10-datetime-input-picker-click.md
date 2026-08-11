# CliniSys — Date/Time Input Picker Click Spec

Date: 2026-08-10
Status: Draft
Issue: [#12](https://github.com/willianbrecher/clinisys/issues/12)

## 1. Goal

Clicking anywhere inside a date/time input field should open the native picker
(calendar/clock), not just the small icon inside the field.

## 2. Current behavior — confirmed

The app uses native `<input type="date">` / `type="time"` / `type="datetime-local">` fields
everywhere, all going through the single shared `Input` component
(`frontend/src/components/ui/input.tsx`), which is a thin wrapper with no click handling —
`type` and all other props pass straight through to the native `<input>`.

Four fields across three files are affected:

| File | Field | Type |
|---|---|---|
| `frontend/src/features/patients/PatientFormContent.tsx:66` | Date of birth | `date` |
| `frontend/src/features/settings/SettingsPage.tsx:106` | Clinic open time | `time` |
| `frontend/src/features/settings/SettingsPage.tsx:111` | Clinic close time | `time` |
| `frontend/src/features/appointments/AppointmentFormContent.tsx:137` | Appointment start | `datetime-local` |

(The issue names the first two as examples; `datetime-local` in the appointment form has the same
native-input behavior and is in scope too.)

Browser-native behavior for these input types: clicking the calendar/clock icon opens the picker;
clicking anywhere else in the field just places the text caret. There is no existing custom
date-picker component in `frontend/src/components/ui` — every date/time field in the app is a bare
native input, so there's nothing to swap out, only click behavior to add.

## 3. Proposed fix

Centralize the fix in the shared `Input` component rather than patching each of the 4 call sites,
since all of them already go through it and any future date/time field will too.

`HTMLInputElement.showPicker()` programmatically opens the native picker on user activation and is
supported by all evergreen browsers. In `frontend/src/components/ui/input.tsx`, wrap the existing
`onClick` for `date` / `time` / `datetime-local` inputs:

```tsx
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
        className={cn(/* unchanged */)}
        ref={ref}
        {...props}
      />
    );
  }
);
```

Notes:
- Guard on `!disabled` — calling `showPicker()` on a disabled element throws
  `InvalidStateError`. This matters for `AppointmentFormContent`, whose start-time field is
  `disabled` in detail/read-only mode.
- `readOnly` inputs are left alone (no guard needed) — `showPicker()` on a read-only field is
  allowed and matches native icon-click behavior already.
- `showPicker?.()` optional-chains the call so browsers without support just keep today's
  icon-only behavior instead of throwing.
- Clicking the icon itself will now trigger `showPicker()` twice in a row (once from the browser's
  native icon handling, once from this handler) — harmless, the second call is a no-op on an
  already-open picker.

## 4. Non-goals

- No visual/styling changes to the inputs.
- No custom date-picker component — native inputs stay native, only the click-to-open behavior
  changes.
- No change to inputs of other types (`text`, `number`, `email`, etc.) — the `PICKER_TYPES` guard
  keeps this scoped to date/time fields only.
