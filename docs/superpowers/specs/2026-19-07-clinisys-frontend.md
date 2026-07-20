# CliniSys — Frontend Design Spec

Date: 2026-07-19
Status: Approved

## 1. Purpose & Scope

React + TypeScript SPA for the CliniSys clinic scheduling system. Used by
clinic Staff, Admin, and Doctors (internal users only — patients never log in).
Communicates with the .NET backend via REST API.

## 2. Tech Stack

- **Framework:** React 18 + TypeScript (Vite)
- **UI:** Shadcn/UI + Tailwind CSS — CSS variables drive all colors; no MUI anywhere
- **Routing:** React Router v6
- **HTTP:** Axios — request interceptor attaches JWT; response interceptor catches 401 → redirect `/login`
- **Forms:** React Hook Form + Yup; schemas colocated per feature
- **Theme:** `next-themes` — dark/light/system; CSS variable layer in `index.css`
- **i18n:** `i18next` + `react-i18next` + `i18next-browser-languagedetector`
- **Calendar:** `@fullcalendar/react`, `@fullcalendar/daygrid`, `@fullcalendar/timegrid`, `@fullcalendar/interaction`
- **Notifications:** Sonner (Shadcn toast)

## 3. Directory Layout

```
frontend/
├── src/
│   ├── api/
│   │   ├── client.ts            # axios instance with interceptors
│   │   ├── appointments.ts
│   │   ├── auth.ts
│   │   ├── clinicSettings.ts
│   │   ├── doctors.ts
│   │   ├── patients.ts
│   │   └── users.ts
│   ├── auth/
│   │   ├── AuthContext.tsx      # JWT decode, role/userId/doctorId, login/logout
│   │   └── ProtectedRoute.tsx   # redirects to /login if unauthenticated; role gate
│   ├── components/
│   │   ├── AppLayout.tsx        # sidebar nav + header; Sheet drawer on mobile
│   │   ├── DataTable.tsx        # generic paginated table / card fallback on mobile
│   │   ├── ConfirmDialog.tsx
│   │   ├── PageSizeSelect.tsx
│   │   └── ThemeToggle.tsx      # Light/Dark/System dropdown, fires PATCH /account/preferences
│   ├── features/
│   │   ├── appointments/
│   │   │   ├── AppointmentsPage.tsx   # tabs: List | Calendar
│   │   │   ├── AppointmentList.tsx
│   │   │   ├── AppointmentCalendar.tsx
│   │   │   ├── AppointmentModal.tsx   # create/edit modal
│   │   │   └── appointment.schema.ts
│   │   ├── patients/
│   │   │   ├── PatientsPage.tsx
│   │   │   ├── PatientForm.tsx
│   │   │   └── patient.schema.ts
│   │   ├── doctors/
│   │   │   ├── DoctorsPage.tsx
│   │   │   └── DoctorForm.tsx
│   │   ├── users/
│   │   │   ├── UsersPage.tsx
│   │   │   └── UserForm.tsx
│   │   ├── settings/
│   │   │   └── SettingsPage.tsx  # clinic hours + logo upload
│   │   └── account/
│   │       └── AccountPage.tsx   # change password + profile picture
│   ├── locales/
│   │   ├── en-US/translation.json
│   │   ├── pt-BR/translation.json
│   │   └── es-ES/translation.json
│   ├── theme/
│   │   └── index.css            # :root and .dark CSS variable overrides; FullCalendar overrides
│   ├── i18n.ts                  # i18next init: languageDetector, resources, fallbackLng: en-US
│   ├── App.tsx                  # router + ThemeProvider + AuthProvider
│   └── main.tsx
├── index.html
├── tailwind.config.ts
├── tsconfig.json
└── vite.config.ts
```

## 4. Routes

All routes except `/login` are wrapped in `ProtectedRoute`. Role gates are
enforced per route; the `ProtectedRoute` component reads the decoded JWT role.

| Path | Component | Roles |
|---|---|---|
| `/login` | `LoginPage` | public |
| `/` | `DashboardPage` | all |
| `/patients` | `PatientsPage` | Admin, Staff |
| `/patients/new` | `PatientForm` | Admin, Staff |
| `/patients/:id` | `PatientForm` | Admin, Staff |
| `/doctors` | `DoctorsPage` | Admin, Staff |
| `/doctors/:id` | `DoctorForm` | Admin, Staff |
| `/appointments` | `AppointmentsPage` | all |
| `/users` | `UsersPage` | Admin |
| `/settings` | `SettingsPage` | Admin |
| `/account` | `AccountPage` | all |

## 5. Authentication Flow

- Login form POSTs `application/x-www-form-urlencoded` to `POST /connect/token`
  with `grant_type=password`, `username`, `password`, `scope=openid`.
- JWT stored in `localStorage` (key: `clinisys_token`).
- `AuthContext` decodes the JWT with `jwt-decode` to expose `role`, `userId`,
  `doctorId`, `fullName`, `theme`, `language`.
- On login, `AuthContext` immediately calls `i18next.changeLanguage(language)` and
  `setTheme(theme)` from the decoded claims.
- Axios request interceptor reads `localStorage` for the token and sets
  `Authorization: Bearer <token>`.
- Axios response interceptor: on `401`, clears token and redirects to `/login`.

## 6. Clinic Logo & User Avatar

- **Login page:** renders `<img src={settings.logoBase64}>` above the form.
  Fallback: Shadcn `Avatar` showing the letter "C".
- **AppLayout header (left):** same logo `h-8 w-8 object-contain` to the left of
  "CliniSys". Same fallback letter "C".
- **AppLayout header (right):** Shadcn `Avatar` — `profilePictureBase64` as image
  src if set; otherwise user's initials (first + last name).
- Upload flow: `<input type="file" accept="image/*">` → `FileReader.readAsDataURL`
  → base64 data URI string → send in request body.
- Client-side guard: reject files > 512 KB before encoding. Show live preview
  before saving. "Remove" button sends `null` to clear the field.

## 7. Theming

- `next-themes` `ThemeProvider` wraps the app at root with `attribute="class"`.
- **Unauthenticated:** defaults to `system` (OS preference).
- **On login:** `AuthContext` reads `theme` claim → `setTheme(theme)`.
  Writes resolved value to `localStorage` as local cache.
- **ThemeToggle** (header, all authenticated pages): Shadcn `Button` +
  `DropdownMenu`, options Light / Dark / System. Calls `setTheme()` immediately;
  fires `PATCH /api/account/preferences` in background (no loading state).
- All color is via Tailwind CSS variables (`bg-background`, `text-foreground`,
  `--primary`, etc.) defined in `index.css` under `:root` and `.dark`.
- No inline color styles anywhere.

## 8. Internationalization (Frontend)

- `i18next` initialized in `i18n.ts` with `LanguageDetector`, `initReactI18next`,
  and all 3 locale JSON bundles. `fallbackLng: "en-US"`.
- All user-visible strings use `useTranslation()` — no hard-coded text in JSX.
- **Unauthenticated:** browser language detector picks locale; falls back to `en-US`.
- **On login:** `AuthContext` reads `language` claim → `i18next.changeLanguage(language)`.
  Writes to `localStorage`.
- **LanguageSwitcher** (header): Shadcn `DropdownMenu`, options English / Português
  / Español. Calls `changeLanguage()` immediately; fires
  `PATCH /api/account/preferences` in background.
- Translation files: `src/locales/<locale>/translation.json`.
  Keys are dot-separated: e.g. `appointments.status.scheduled`.
  All three files must stay in sync — missing keys fall back to `en-US`.
- Dates, times, and numbers: `Intl.DateTimeFormat` / `Intl.NumberFormat` with
  the active locale. No date-fns or moment.

## 9. Appointment Calendar

- Built with `@fullcalendar/react`. Default view: `timeGridWeek`.
  Built-in toolbar lets users switch to `timeGridDay` or `dayGridMonth`.
- **Clinic hours enforcement:**
  - `slotMinTime` = `ClinicSettings.OpenTime`
  - `slotMaxTime` = `ClinicSettings.CloseTime`
  - `businessHours` = clinic's `OpenDays` mapped to FullCalendar's `daysOfWeek`
  - `selectConstraint: "businessHours"` blocks slot selection outside open hours/days
- **Events:** color-coded by status —
  `Scheduled` → blue, `Confirmed` → green, `Completed` → slate,
  `Cancelled` → red, `NoShow` → orange.
  Event title: `"<patient name> — Dr. <doctor name>"`.
- **Scheduling from calendar (Staff/Admin):** clicking an empty slot opens
  `AppointmentModal` pre-filled with the clicked `start` date/time and a 30-min
  default duration. Blocked outside clinic hours client-side; backend re-validates.
- **Editing:** clicking an existing event opens `AppointmentModal` in edit mode.
  Drag-and-drop is out of scope for v1.
- **Data fetching:** `GET /api/appointments?startDate=&endDate=` on every view
  navigation. Results cached per visible date range in component state; back-nav
  to a loaded range does not re-fetch.
- **FullCalendar theming:** CSS variable overrides in `index.css` under `:root`
  and `.dark` to match the Shadcn palette.

## 10. Responsive Layout

- Mobile-first Tailwind breakpoints: `sm` 640 px, `md` 768 px, `lg` 1024 px.
- **AppLayout:** `lg+` — fixed sidebar nav always visible.
  Below `lg` — sidebar hidden; hamburger button in header opens a Shadcn `Sheet`
  (slide-in drawer).
- **DataTable:** `md+` — standard `<table>`. Below `md` — each row becomes a
  label/value card, or the table gets a horizontal scroll wrapper (decided per
  feature by column count).
- **Forms:** single-column on mobile; two-column grid on `md+` for forms with
  many fields (patient create/edit).
- **Modals / dialogs:** on mobile, Shadcn `Dialog` renders full-screen
  (Shadcn `Sheet` bottom variant) so keyboard doesn't obscure inputs.
- No feature is hidden on mobile — everything accessible on desktop must be
  accessible on mobile, just reflowed.

## 11. Error Handling (Frontend)

- Axios response interceptor: catches errors and normalizes to
  `{ message: string, errors?: string[] }` shape.
- Transient notifications: Sonner (Shadcn toast) — fires on every API error.
- Inline field errors: React Hook Form + Yup for client-side; for `400` responses
  from the backend, map `errors[]` to form field errors by field name where possible,
  otherwise show in toast.
- `401`: clear token, redirect to `/login`.
- `403`: show toast "You don't have permission to perform this action."
- `404`: show toast "Resource not found."
- `409`: show toast with the server message (e.g. "This time slot is already booked.").
- `500`: show generic toast "An unexpected error occurred."
