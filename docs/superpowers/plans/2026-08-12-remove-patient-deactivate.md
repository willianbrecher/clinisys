# Remove Patient "Deactivate" Action Implementation Plan

> Implement task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the "Deactivate" action from the patient list/record (#11) — the button, its
handler, and the API call it triggers.
Spec: `docs/superpowers/specs/2026-08-12-remove-patient-deactivate.md`.

**Tech Stack:** React 18, TypeScript, react-i18next (frontend only — no backend changes; the
`DeactivatePatientCommand`/handler/controller action stay in place per the spec's non-goals).

## Global Constraints

- Follow root `CLAUDE.md`: branch `feature/<slug>` referencing issue #11.
- Single-layer (frontend-only) change → PR uses `Closes #11`.
- Repo has no `feature` label (confirmed via `gh label list`) — use `enhancement`, matching the
  issue's own label, per precedent from PR #24/#26/#27.

---

### Task 1: Remove the "Deactivate" action from the patient list (#11)

**Branch:** `feature/remove-patient-deactivate` → PR `Closes #11`

**Files:**
- Modify: `frontend/src/features/patients/PatientsPage.tsx`
- Modify: `frontend/src/api/patients.ts`
- Modify: `frontend/src/locales/en-US/translation.json`
- Modify: `frontend/src/locales/es-ES/translation.json`
- Modify: `frontend/src/locales/pt-BR/translation.json`

**Interfaces:** none — pure removal, no new exports or props.

- [ ] **Step 1: Remove `deactivatePatient` from the API client**

In `frontend/src/api/patients.ts`, delete lines 18-19:

```tsx
export const deactivatePatient = (id: string) =>
  client.delete(`/api/patients/${id}`);
```

- [ ] **Step 2: Remove the handler and both buttons from `PatientsPage.tsx`**

In `frontend/src/features/patients/PatientsPage.tsx`:

1. Change the import on line 12 to drop `deactivatePatient`, keeping `getPatients`:

```tsx
import { getPatients } from "@/api/patients";
```

2. Delete the `handleDeactivate` function (lines 33-41):

```tsx
const handleDeactivate = async (id: string) => {
  try {
    await deactivatePatient(id);
    toast.success("Patient deactivated.");
    load();
  } catch {
    toast.error("Failed to deactivate patient.");
  }
};
```

3. Delete the desktop-table "Deactivate" button (lines 79-81), leaving only the "Edit" button in
   the row's action cell:

```tsx
<Button size="sm" variant="destructive" onClick={() => handleDeactivate(p.id)}>
  {t("patients.deactivate")}
</Button>
```

4. Delete the mobile-card "Deactivate" button (lines 103-105), leaving only the "Edit" button:

```tsx
<Button size="sm" variant="destructive" className="flex-1" onClick={() => handleDeactivate(p.id)}>
  {t("patients.deactivate")}
</Button>
```

Since "Edit" becomes the sole action, drop the now-unnecessary `flex-1` sizing tweak only if it
was solely to share space with "Deactivate" — check visually in Step 4; otherwise leave the
wrapping `<div className="flex gap-2 ...">` as-is, it still works with one child.

- [ ] **Step 3: Remove the now-unused locale keys**

Delete `"deactivate"` and `"deactivateConfirm"` from the `patients` block (lines 52-53) in all
three bundles:

`frontend/src/locales/en-US/translation.json`:
```json
"deactivate": "Deactivate Patient",
"deactivateConfirm": "Are you sure you want to deactivate this patient?",
```

`frontend/src/locales/es-ES/translation.json` and `frontend/src/locales/pt-BR/translation.json`:
same two keys, their respective translated values.

- [ ] **Step 4: Manually verify via the `run` skill**

- Patients list (desktop table width): each row shows only "Edit", no "Deactivate" button.
- Patients list (mobile/narrow width): each card shows only "Edit", no "Deactivate" button.
- Editing a patient via "Edit" still opens the modal and saves correctly (unaffected by this
  change, confirming nothing else broke).
- No console errors on page load (confirms the locale key removal didn't leave a dangling
  reference — `grep` for `patients.deactivate` across `frontend/src` should return nothing).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/patients/PatientsPage.tsx frontend/src/api/patients.ts frontend/src/locales/en-US/translation.json frontend/src/locales/es-ES/translation.json frontend/src/locales/pt-BR/translation.json
git commit -m "fix: remove Deactivate action from patient list"
```

- [ ] **Step 6: Open PR**

```bash
gh pr create --title "fix: remove Deactivate action from patient list" \
  --body "Closes #11

Removes the \"Deactivate\" button, its handler, and the \`deactivatePatient\` API client call from the patient list/record. Backend \`DeactivatePatientCommand\`/controller action are left in place — out of scope per the issue.

Spec: \`docs/superpowers/specs/2026-08-12-remove-patient-deactivate.md\`" \
  --label enhancement --assignee willianbrecher
```
