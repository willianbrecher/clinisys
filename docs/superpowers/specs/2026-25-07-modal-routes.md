# CliniSys — Modal Routes Design Spec

Date: 2026-07-25
Status: Approved

## 1. Goal

Standardize all create/edit/detail forms to open inside a Shadcn `Dialog` modal while updating the browser URL. The list page stays mounted and visible behind the modal. Back-button and direct URL navigation both work correctly.

## 2. Approach

React Router v6 **nested routes + `<Outlet>` inside `<Dialog>`**.

Each list page renders `<Outlet />` wrapped in a `<Dialog>`. Child routes supply only form content (no page shell). Closing the dialog navigates to the parent route. The browser's back button natively closes the modal by popping the history entry.

## 3. URL Structure

| URL | Modal content | Who can open it |
|---|---|---|
| `/patients/new` | Create patient form | Admin, Staff |
| `/patients/:id/edit` | Edit patient form | Admin, Staff |
| `/doctors/:id/edit` | Edit doctor specialty | Admin |
| `/doctors/:id/detail` | View doctor (read-only) | Staff |
| `/users/new` | Create user form | Admin |
| `/appointments/new` | Create appointment form | Admin, Staff |
| `/appointments/:id/edit` | Edit / reschedule appointment | Admin, Staff |
| `/appointments/:id/detail` | View appointment (read-only) | Doctor |

**Not routed:** Reset-password dialog in `UsersPage` stays state-driven (secondary sub-action, no deep-link value).

## 4. Component Contract

### List page (host)

```tsx
const outlet = useOutlet();
const navigate = useNavigate();
const close = () => navigate('/patients');    // always back to the list

return (
  <>
    {/* list JSX */}
    <Dialog open={!!outlet} onOpenChange={(open) => { if (!open) close(); }}>
      <DialogContent className="max-w-lg">
        <Outlet context={{ onClose: close, onSaved: load }} />
      </DialogContent>
    </Dialog>
  </>
);
```

### Form content component (guest)

```tsx
const { onClose, onSaved } = useOutletContext<ModalContext>();

// success → refresh list then close
onSaved();
onClose();

// cancel → just close
onClose();
```

### Detail content component (read-only guest)

Receives only `onClose`. All inputs carry `disabled` and `readOnly`. No save button — only a Close button.

```tsx
const { onClose } = useOutletContext<{ onClose: () => void }>();
```

### Shared type

```ts
// src/types/modal.ts
export interface ModalContext {
  onClose: () => void;
  onSaved: () => void;
}
```

## 5. Route Tree Changes (`App.tsx`)

Remove flat sibling routes; convert to nested children:

```tsx
<Route path="patients"
  element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><PatientsPage /></ProtectedRoute>}>
  <Route path="new"      element={<PatientFormContent />} />
  <Route path=":id/edit" element={<PatientFormContent />} />
</Route>

<Route path="doctors"
  element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><DoctorsPage /></ProtectedRoute>}>
  <Route path=":id/edit"   element={<DoctorFormContent />} />
  <Route path=":id/detail" element={<DoctorFormContent />} />
</Route>

<Route path="users"
  element={<ProtectedRoute allowedRoles={["Admin"]}><UsersPage /></ProtectedRoute>}>
  <Route path="new" element={<UserFormContent />} />
</Route>

<Route path="appointments" element={<AppointmentsPage />}>
  <Route path="new"        element={<AppointmentFormContent />} />
  <Route path=":id/edit"   element={<AppointmentFormContent />} />
  <Route path=":id/detail" element={<AppointmentFormContent />} />
</Route>
```

Role gates sit on the parent only. Child routes inherit protection.

## 6. Navigation Logic Per Feature

### PatientsPage
```ts
navigate('/patients/new')           // New button
navigate(`/patients/${id}/edit`)    // Edit button (Admin/Staff always)
```

### DoctorsPage
```ts
navigate(`/doctors/${id}/edit`)     // role === "Admin"
navigate(`/doctors/${id}/detail`)   // role === "Staff"
```

### UsersPage
```ts
navigate('/users/new')              // New button
// Reset-password stays as <Dialog open={!!resetTarget}>
```

### AppointmentsPage
```ts
navigate('/appointments/new')                   // New button / calendar slot click
navigate(`/appointments/${id}/edit`)            // role !== "Doctor"
navigate(`/appointments/${id}/detail`)          // role === "Doctor"
```

## 7. Form Components — Refactor Summary

Each existing form component becomes a **content-only component** that:
- Reads `{ onClose, onSaved }` from `useOutletContext()` instead of calling `navigate()`
- Detects edit vs create via `useParams()` (unchanged)
- Detects detail/read-only via `useMatch('*/detail')` — disables all inputs and hides the save button

| Old file | New file | Change |
|---|---|---|
| `PatientForm.tsx` | `PatientFormContent.tsx` | Remove `useNavigate`; use outlet context |
| `DoctorForm.tsx` | `DoctorFormContent.tsx` | Remove `useNavigate`; add read-only mode |
| `UserForm.tsx` | `UserFormContent.tsx` | Move submit logic in (currently in `UsersPage.handleCreate`); use outlet context |
| `AppointmentModal.tsx` | `AppointmentFormContent.tsx` | Remove `<Dialog>` wrapper and open/onClose props; use outlet context |

### UserFormContent note
Currently `UserForm` is a controlled component — the submit handler lives in `UsersPage`. In the new design, `UserFormContent` owns the `createUser()` call directly and uses outlet context, consistent with all other form components.

## 8. Passing Data Into Appointment Modals

There is no `GET /api/appointments/:id` endpoint, so `AppointmentFormContent` cannot fetch by id. Data is passed via React Router navigation state instead.

**Calendar slot click (new):**
```ts
navigate('/appointments/new', { state: { defaultStartsAt: info.dateStr } });
```

**List/calendar event click (edit or detail):**
```ts
navigate(`/appointments/${appt.id}/edit`, { state: { appointment: appt } });
// or
navigate(`/appointments/${appt.id}/detail`, { state: { appointment: appt } });
```

**Inside `AppointmentFormContent`:**
```ts
const { state } = useLocation();
const defaultStartsAt: string | undefined = state?.defaultStartsAt;
const appointment: AppointmentModel | undefined = state?.appointment;
```

Direct URL navigation to `/appointments/:id/edit` without state results in an empty form (acceptable edge case — no backend single-fetch endpoint exists).

## 9. Dialog Sizing

| Feature | `DialogContent` max-width |
|---|---|
| PatientFormContent | `max-w-lg` |
| DoctorFormContent | `max-w-sm` |
| UserFormContent | `max-w-md` |
| AppointmentFormContent | `max-w-lg` |

## 10. Back-Button & Direct URL Behavior

- **Back button from modal:** React Router pops `/patients/new` → `/patients`. The parent `PatientsPage` is never unmounted, so the list is still loaded. `useOutlet()` returns `null` → Dialog closes. No extra code needed.
- **Direct URL to `/patients/new`:** Router renders `PatientsPage` (list loads) with `PatientFormContent` as the outlet. `useOutlet()` returns the content → Dialog opens immediately.
- **Direct URL to `/patients/:id/edit`:** Same as above; `PatientFormContent` reads `:id` from `useParams()` and fetches the patient.

## 11. What Does Not Change

- `AccountPage`, `SettingsPage`, `LoginPage`, `DashboardPage` — no form/list pattern
- All Yup validation schemas (`*.schema.ts`) — unchanged
- All API modules (`src/api/`) — unchanged
- Reset-password dialog in `UsersPage` — stays state-driven
- Appointment calendar slot-click behavior — still calls `navigate('/appointments/new')`
- Mobile card/table responsive layout inside list pages — unchanged
