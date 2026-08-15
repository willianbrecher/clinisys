# Calendar and Date UI Language Implementation Plan

> Implement task-by-task, in order. Steps use checkbox (`- [ ]`) syntax for tracking. **Merge each
> PR to `master` before starting the next task** where noted — tasks 1 and 3 both touch
> `AppointmentsPage.tsx` (different concerns), so branching each off the latest `master` avoids
> conflicts.

**Goal:** Make the FullCalendar appointments calendar, native date/time inputs, list/detail date
formatting, and the Settings open-days labels all follow the app's selected language.
Spec: `docs/superpowers/specs/2026-08-15-calendar-date-ui-language.md`.

**Tech Stack:** React 18/TypeScript/react-i18next/FullCalendar (frontend only).

## Global Constraints

- Follow root `CLAUDE.md`: one branch (`fix/<slug>`) per PR, referencing #39.
- All four fixes are frontend-only, but #39 is one issue covering all of them — each task's PR
  uses `Refs #39` (not `Closes #39` on any single one, since none of them alone fully resolves the
  issue); close #39 manually once all four tasks have merged.
- Issue is `bug`-labeled — use `bug` on all PRs.
- Implementation order: **Task 1 (`<html lang>` sync) → Task 2 (FullCalendar locale + list/detail
  dates) → Task 3 (Settings day labels)**. Task 1 is a standalone file (`i18n.ts`) with zero
  overlap with the others, so it can land first without affecting sequencing. Tasks 2 and 3 touch
  different files (`AppointmentsPage.tsx` vs `SettingsPage.tsx`) and could be done in either order
  or even combined, but are kept separate to keep each diff scoped to one concern.

---

### Task 1: Sync `<html lang>` with the selected language

**Branch:** `fix/html-lang-sync` → PR `Refs #39`

**Files:**
- Modify: `frontend/src/i18n.ts`

**Interfaces:** none — side-effecting init code only.

- [ ] **Step 1: Set `document.documentElement.lang` on init and on every language change**

In `frontend/src/i18n.ts`, after the existing `.init({...})` call:

```ts
i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      "en-US": { translation: enUS },
      "pt-BR": { translation: ptBR },
      "es-ES": { translation: esES },
    },
    fallbackLng: "en-US",
    supportedLngs: ["en-US", "pt-BR", "es-ES"],
    interpolation: { escapeValue: false },
    detection: {
      order: ["localStorage", "navigator"],
      caches: ["localStorage"],
      lookupLocalStorage: "clinisys_language",
    },
  });

document.documentElement.lang = i18n.language;
i18n.on("languageChanged", (lng) => {
  document.documentElement.lang = lng;
});

export default i18n;
```

(`i18n.language` is synchronously available immediately after `.init()` resolves the detected/
persisted language — no need to wait for a promise here, matching how `i18n` is used as a
side-effecting singleton import elsewhere in the app.)

- [ ] **Step 2: Manually verify via the `run` skill**

- On load, `document.documentElement.lang` (inspect via devtools or `document.documentElement.lang`
  in the console) matches the persisted/detected language (`en-US`, `pt-BR`, or `es-ES`).
- Switching language via the header `LanguageSwitcher` or Account page updates
  `document.documentElement.lang` immediately.
- No visible regression elsewhere (this attribute isn't read by any existing app code, only by the
  browser itself for native picker rendering) — general smoke pass through a few pages.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/i18n.ts
git commit -m "fix: sync document lang attribute with the selected app language"
```

- [ ] **Step 4: Open PR**

```bash
gh pr create --title "fix: sync document lang attribute with the selected app language" \
  --body "Refs #39

\`<html lang>\` was hardcoded to \`en\` in \`index.html\` and never updated — nothing in the app touched \`document.documentElement.lang\` on language change. Chromium-based browsers use the document language (not just OS locale) to decide what language to render some native form controls' picker UI in, including \`<input type=\"datetime-local\">\`/\`<input type=\"time\">\`. Syncs it on init and on every \`i18n.on(\"languageChanged\", ...)\` event.

Spec: \`docs/superpowers/specs/2026-08-15-calendar-date-ui-language.md\`" \
  --label bug --assignee willianbrecher
```

---

### Task 2: FullCalendar locale + list/detail date formatting

**Branch:** `fix/calendar-locale` → PR `Refs #39`. Branch from `master` after Task 1's PR merges
(no file overlap, but keeps a clean sequential history).

**Files:**
- Modify: `frontend/src/features/appointments/AppointmentsPage.tsx`

**Interfaces:** none.

- [ ] **Step 1: Import FullCalendar locale modules and destructure `i18n`**

```tsx
import { useTranslation } from "react-i18next";
import esLocale from "@fullcalendar/core/locales/es";
import ptBrLocale from "@fullcalendar/core/locales/pt-br";
```

```tsx
const { t, i18n } = useTranslation();
```

- [ ] **Step 2: Add a locale-code map and wire `locale`/`locales` onto `<FullCalendar>`**

Near the other derived values (`openDays`/`slotMinTime`/`slotMaxTime`):

```tsx
const FC_LOCALE_MAP: Record<string, string> = { "pt-BR": "pt-br", "es-ES": "es" };
const fcLocale = FC_LOCALE_MAP[i18n.language] ?? "en";
```

Add to `<FullCalendar>`'s props:

```tsx
locales={[esLocale, ptBrLocale]}
locale={fcLocale}
```

- [ ] **Step 3: Localize the list/detail date formatting**

Replace both occurrences (`:143` and `:171`) of:

```tsx
new Intl.DateTimeFormat(undefined, { dateStyle: "short", timeStyle: "short" }).format(new Date(a.startsAt))
```

with:

```tsx
new Intl.DateTimeFormat(i18n.language, { dateStyle: "short", timeStyle: "short" }).format(new Date(a.startsAt))
```

- [ ] **Step 4: Manually verify via the `run` skill**

- Switch language to Portuguese — calendar toolbar buttons ("Hoje", "Mês", "Semana", "Dia"),
  weekday headers, and month title render in Portuguese.
- Switch to Spanish — same, in Spanish.
- Switch back to English (`en-US`) — calendar renders in English (FullCalendar's built-in default,
  no import needed for this one).
- List view and detail view's formatted date/time strings visibly change format/language
  alongside the calendar (e.g. Portuguese's `dd/mm/aaaa` vs. English's `mm/dd/yyyy` ordering).
- Calendar's existing behavior (validRange, businessHours, hiddenDays, dateClick guards from #31/
  #38) is unaffected by the locale change — spot-check a past-date click is still blocked.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/appointments/AppointmentsPage.tsx
git commit -m "fix: make appointments calendar and date display follow the selected language"
```

- [ ] **Step 6: Open PR**

```bash
gh pr create --title "fix: make appointments calendar and date display follow the selected language" \
  --body "Refs #39

FullCalendar rendered its chrome (toolbar, weekday/month names) in English regardless of the app's selected language — no \`locale\`/\`locales\` prop was ever set, and no FullCalendar locale modules were imported. Imports \`@fullcalendar/core/locales/es\` and \`.../pt-br\`, and derives \`locale\` from \`i18n.language\` (mapped to FullCalendar's lowercase codes). Also switches the list/detail date formatting from \`Intl.DateTimeFormat(undefined, ...)\` to \`Intl.DateTimeFormat(i18n.language, ...)\`, which had the same gap.

Spec: \`docs/superpowers/specs/2026-08-15-calendar-date-ui-language.md\`" \
  --label bug --assignee willianbrecher
```

---

### Task 3: Translate Settings' open-days labels

**Branch:** `fix/settings-open-days-i18n` → PR `Refs #39`. Branch from `master` after Task 2's PR
merges (no file overlap with Task 2, but keeps a clean sequential history).

**Files:**
- Modify: `frontend/src/features/settings/SettingsPage.tsx`

**Interfaces:** none. Reuses existing locale keys `settings.day_0`–`settings.day_6`, already present
and correct in all three bundles — no locale file changes.

- [ ] **Step 1: Replace hardcoded day names with translation keys**

Replace:

```tsx
const DAYS = [
  { value: 1, label: "Monday" },
  { value: 2, label: "Tuesday" },
  { value: 3, label: "Wednesday" },
  { value: 4, label: "Thursday" },
  { value: 5, label: "Friday" },
  { value: 6, label: "Saturday" },
  { value: 0, label: "Sunday" },
];
```

with:

```tsx
const DAYS = [
  { value: 1, labelKey: "settings.day_1" },
  { value: 2, labelKey: "settings.day_2" },
  { value: 3, labelKey: "settings.day_3" },
  { value: 4, labelKey: "settings.day_4" },
  { value: 5, labelKey: "settings.day_5" },
  { value: 6, labelKey: "settings.day_6" },
  { value: 0, labelKey: "settings.day_0" },
];
```

- [ ] **Step 2: Render the translated label**

Replace (`:126`):

```tsx
<label htmlFor={`day-${d.value}`} className="text-sm cursor-pointer">{d.label}</label>
```

with:

```tsx
<label htmlFor={`day-${d.value}`} className="text-sm cursor-pointer">{t(d.labelKey)}</label>
```

- [ ] **Step 3: Manually verify via the `run` skill**

- Settings page, English — day checkboxes read "Monday" through "Sunday" (unchanged from today).
- Switch to Portuguese — day checkboxes read "Segunda-feira" through "Domingo".
- Switch to Spanish — day checkboxes read "Lunes" through "Domingo".
- Toggling a day and saving still persists correctly (unaffected — `value`/CSV logic untouched).

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/settings/SettingsPage.tsx
git commit -m "fix: translate open-days checkbox labels on the Settings page"
```

- [ ] **Step 5: Open PR**

```bash
gh pr create --title "fix: translate open-days checkbox labels on the Settings page" \
  --body "Refs #39

The clinic-hours open-days checkboxes were labeled with hardcoded English day names, never run through \`t(...)\` — despite the \`settings.day_0\`–\`settings.day_6\` translation keys already existing (and being correct) in all three locale bundles. Wires the existing keys into the component; no locale file changes needed.

Spec: \`docs/superpowers/specs/2026-08-15-calendar-date-ui-language.md\`" \
  --label bug --assignee willianbrecher
```

**After this PR merges**, close #39 manually — all four gaps it described (FullCalendar, native
input `<html lang>` sync, list/detail date formatting, Settings day labels) are addressed.
