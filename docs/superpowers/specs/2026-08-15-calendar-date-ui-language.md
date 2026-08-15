# CliniSys — Calendar and Date UI Language Spec

Date: 2026-08-15
Status: Draft
Issue: [#39](https://github.com/willianbrecher/clinisys/issues/39)

## 1. Goal

Every calendar/date-related UI element in the app should follow the currently selected app
language, the same way translated text already does via `react-i18next`. Today four distinct spots
don't, for four distinct reasons — this spec addresses all four, since #39 explicitly bundles them
as one issue.

## 2. Current behavior — confirmed

### 2.1 Event calendar (FullCalendar) — `frontend/src/features/appointments/AppointmentsPage.tsx`

The `<FullCalendar>` element (`:192-224`, current line numbers after #31/#38's merged fixes) sets
`plugins`, `initialView`, `headerToolbar`, `validRange`, `businessHours`, `slotMinTime`/
`slotMaxTime`, `hiddenDays`, `selectable`, `selectConstraint`, `dateClick`, `eventClick`, `events`,
`height` — but no `locale`/`locales` prop. `useTranslation()` (`:30`) is only used to destructure
`t`; `i18n.language` is never read here. `@fullcalendar/core` is pinned at `6.1.21`
(`frontend/package.json:13`); no `@fullcalendar/core/locales/*` module is imported anywhere in the
repo. FullCalendar's locale codes are lowercase/hyphenated (`en`, `es`, `pt-br`) — the app's
i18next codes (`en-US`, `pt-BR`, `es-ES`, from `frontend/src/i18n.ts:14-16`) don't match directly.

### 2.2 Native date/time inputs — driven by `<html lang>`, not by the app

- `AppointmentFormContent.tsx` — `startsAt`, `<input type="datetime-local">`.
- `SettingsPage.tsx:106,111` — `openTime`/`closeTime`, `<input type="time">`.

Both are genuine native inputs — no date-picker library, no custom formatting logic anywhere in
the repo (`components/ui/input.tsx`'s only non-passthrough logic is the unrelated
click-to-open-picker fix). Their picker widget's rendered language (month/day names, AM/PM vs 24h)
follows the *document* language in Chromium-based browsers, but `frontend/index.html:2` hardcodes
`<html lang="en">`, and nothing in the app ever updates `document.documentElement.lang`
(confirmed: no reference anywhere in `frontend/src`, including `i18n.ts` and
`components/LanguageSwitcher.tsx`, which only calls `i18n.changeLanguage(...)`). So these native
pickers stay rendered as English regardless of the selected app language.

### 2.3 List/detail date display — `AppointmentsPage.tsx:143,171`

```tsx
new Intl.DateTimeFormat(undefined, { dateStyle: "short", timeStyle: "short" }).format(...)
```

`undefined` resolves to the browser's default locale, not `i18n.language` — the formatted
date/time in the appointments list and detail view ignores the in-app language selection.

### 2.4 Settings' open-days checkboxes — `SettingsPage.tsx:13-21,119-128`

```tsx
const DAYS = [
  { value: 1, label: "Monday" },
  { value: 2, label: "Tuesday" },
  ...
];
```

rendered as `{d.label}` (`:126`) — hardcoded English strings, never passed through `t(...)`.
**Correction to #39's original text**: the translation keys already exist and are already correct
in all three bundles (`settings.day_0` through `settings.day_6` in
`frontend/src/locales/{en-US,pt-BR,es-ES}/translation.json`, e.g. `"day_1": "Monday"` / `"Segunda-feira"`
/ `"Lunes"`) — they're simply never referenced. This is a pure wiring gap, not a missing-content gap;
no locale bundle changes are needed.

## 3. Proposed fix

Four independent, narrowly-scoped changes — no shared mechanism between them beyond all reading
`i18n.language`, so no new abstraction is introduced.

### 3.1 FullCalendar locale

Import `@fullcalendar/core/locales/es` and `@fullcalendar/core/locales/pt-br`, pass both via the
`locales` prop, and pass `locale` derived from `i18n.language` mapped to FullCalendar's codes (only
`pt-BR`→`pt-br` and `es-ES`→`es` need mapping; `en-US`→FullCalendar's built-in `en` default needs no
import). Read `i18n.language` via the `i18n` instance already available from `useTranslation()`
(`const { t, i18n } = useTranslation();`) so the component re-renders on language change.

### 3.2 `<html lang>` sync

Add a language-change hook in `frontend/src/i18n.ts`, the single owner of the i18next instance:
set `document.documentElement.lang = i18n.language` once after `.init()`, and again on every
`i18n.on("languageChanged", ...)` event (no such listener exists anywhere in the app today). This
is the one fix that helps both native inputs (2.2) at once, in browsers that honor document-language
for picker rendering — it's a best-effort improvement, not a guarantee, since picker language is
ultimately a browser implementation detail outside the app's control.

### 3.3 List/detail date formatting

Change `Intl.DateTimeFormat(undefined, ...)` to `Intl.DateTimeFormat(i18n.language, ...)` at both
call sites in `AppointmentsPage.tsx`. `useTranslation()` already gives access to `i18n` (needed for
3.1 in the same file).

### 3.4 Settings open-days labels

Replace the hardcoded `label` field in `DAYS` with an i18n key reference, and call `t(...)` at the
render site instead of rendering `d.label` directly — reusing the `settings.day_N` keys that already
exist in all three locale bundles (no locale file changes).

## 4. Non-goals

- No FullCalendar locale beyond `en`/`es`/`pt-br` — matches the app's three supported languages,
  nothing more.
- No polyfill or fallback for browsers that don't honor `<html lang>` for native picker rendering
  (e.g. Firefox/Safari, which are OS-locale-driven regardless) — 3.2 is additive best-effort, not a
  cross-browser guarantee, consistent with how #39 itself scoped this.
- No locale-bundle changes for 3.4 — the keys already exist; this is a component-wiring fix only.
- No change to how `startsAt`/`openTime`/`closeTime` values are read/written (still raw
  ISO-ish strings per the existing `Kind=Utc` backend convention) — this spec only touches how
  things are *displayed*, not any data representation.
