# CLAUDE.md — frontend

Guidance for Claude Code when working under `frontend/`. See the root `CLAUDE.md` for repo-wide
PR/branch/comment conventions that apply here too.

## What this is

The CliniSys web app: React 18 + TypeScript on Vite, Tailwind CSS + Shadcn/UI, FullCalendar for
appointment scheduling, React Hook Form + Yup for forms, react-i18next for localization
(English, Brazilian Portuguese, Spanish).

## Commands

Run from `frontend/`:

```bash
npm run dev       # Vite dev server (http://localhost:5173), proxies /api and /connect to the backend
npm run build     # tsc -b && vite build
npm run lint       # oxlint
npm run preview
```

No test runner is configured (no test script in `package.json`).

`VITE_BACKEND_URL` controls where the Vite proxy sends `/api` and `/connect` (default
`http://localhost:5110`).

## Architecture — feature-folder React

- **`src/api/`** — one file per resource (`patients.ts`, `doctors.ts`, `appointments.ts`,
  `users.ts`, `account.ts`, `clinicSettings.ts`, `auth.ts`) plus a shared Axios client
  (`client.ts`) and shared `types.ts`. This is the only layer that talks to the backend.
- **`src/features/<name>/`** — one folder per page/feature (`appointments`, `patients`, `doctors`,
  `users`, `settings`, `dashboard`, `account`, `auth`) — colocates a feature's components, hooks,
  and logic.
- **`src/auth/`** — `AuthContext` (session/JWT state) and `ProtectedRoute` (role-gated routing).
- **`src/components/`** — shared layout and UI (Shadcn/UI-based) components, not tied to one
  feature.
- **`src/locales/{en-US,es-ES,pt-BR}`** — react-i18next translation bundles; keep these in sync
  with the backend's `CliniSys.Application/Locales` set when adding user-facing strings.

Path alias `@` → `frontend/src` (configured in `vite.config.ts`).

Calendar UI is FullCalendar (`@fullcalendar/*`); these packages must stay in Vite's
`optimizeDeps.include` list in `vite.config.ts` or dev-server prebundling breaks — check that list
before upgrading FullCalendar packages.
