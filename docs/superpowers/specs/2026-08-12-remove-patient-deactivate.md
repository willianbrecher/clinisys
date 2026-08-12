# CliniSys — Remove "Deactivate" Action from Patient List Spec

Date: 2026-08-12
Status: Draft
Issues: [#11](https://github.com/willianbrecher/clinisys/issues/11)

## 1. Goal

Remove the "Deactivate" action from the patient record/list (#11). The button, its handler, and
the API call it triggers all go away. Frontend-only — issue #11's own scope note says "no
implementation included in this issue" and only describes `PatientsPage.tsx`'s button/handler;
nothing in the issue asks for the backend endpoint to be removed.

## 2. Current behavior — confirmed

Full relevant source: `frontend/src/features/patients/PatientsPage.tsx`,
`frontend/src/api/patients.ts`.

`PatientsPage.tsx` imports `deactivatePatient` alongside `getPatients` (line 12) and defines a
handler that calls it, toasts, and reloads the list (lines 33-41):

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

The handler is wired to two "Deactivate" buttons — one in the desktop table row (lines 79-81),
one in the mobile card layout (lines 103-105) — both `variant="destructive"`, both labeled
`t("patients.deactivate")`:

```tsx
<Button size="sm" variant="destructive" onClick={() => handleDeactivate(p.id)}>
  {t("patients.deactivate")}
</Button>
```

`frontend/src/api/patients.ts:18-19` defines the API client method the handler calls:

```tsx
export const deactivatePatient = (id: string) =>
  client.delete(`/api/patients/${id}`);
```

Confirmed via repo-wide grep: `handleDeactivate` and `deactivatePatient` have no other callers —
removing them orphans nothing else in the frontend.

`patients.deactivate` / `patients.deactivateConfirm` locale keys exist in all three bundles
(`en-US`, `es-ES`, `pt-BR` `translation.json:52-53`). `deactivateConfirm` is already dead — no
confirm dialog is wired up in `PatientsPage.tsx` today, so its removal isn't a behavior change.

`PatientModel.isActive` (`frontend/src/api/types.ts:20`) is the field this action used to flip.
It stays — it's a general model field, not exclusive to this button, and nothing in #11 asks to
remove the active/inactive concept itself.

## 3. Backend — out of scope, left as-is

`PatientsController.cs` (`Deactivate` action, `DELETE`-mapped), `DeactivatePatientCommand`, and
`DeactivatePatientCommandHandler` all continue to exist after this change. Issue #11 only
describes removing the frontend action; the endpoint becoming unreachable from the UI is expected,
not a bug. If the backend endpoint should also be removed, that's a separate issue/PR per root
`CLAUDE.md`'s backend/frontend PR split — not bundled here.

There's a separate, unrelated "Deactivate User" feature (`UsersPage.tsx`, `api/users.ts`,
`UsersController.cs`) — not touched by #11 and not touched by this spec.

## 4. Changes

1. `frontend/src/features/patients/PatientsPage.tsx`
   - Drop `deactivatePatient` from the import on line 12, keep `getPatients`.
   - Delete the `handleDeactivate` function (lines 33-41).
   - Delete both "Deactivate" `<Button>` blocks (desktop lines 79-81, mobile lines 103-105),
     leaving the remaining "Edit" button as the sole action in each row/card.
2. `frontend/src/api/patients.ts`
   - Delete the `deactivatePatient` export (lines 18-19).
3. `frontend/src/locales/{en-US,es-ES,pt-BR}/translation.json`
   - Delete the `patients.deactivate` and `patients.deactivateConfirm` keys (lines 52-53 in each).

## 5. Non-goals

- No backend change — `DeactivatePatientCommand`/handler/controller action stay in place.
- No change to `PatientModel.isActive` or any other active/inactive display logic.
- No confirm-dialog work — `deactivateConfirm` was already unused dead copy, not a feature being
  cut short.
- Does not touch the unrelated "Deactivate User" action on `UsersPage.tsx`.
