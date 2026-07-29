# CliniSys

An open-source clinic management system for small to medium medical practices. CliniSys handles patient records, doctor profiles, and appointment scheduling through a clean web interface — all self-hosted with Docker.

![License](https://img.shields.io/badge/license-Apache%202.0-blue) ![.NET](https://img.shields.io/badge/.NET-8-purple) ![React](https://img.shields.io/badge/React-18-blue)

---

## What it does

CliniSys gives a clinic everything it needs to manage its day-to-day operations:

- **Patient records** — register patients with personal details, date of birth, contact info, and notes
- **Doctor profiles** — link doctor accounts to their specialty and manage their availability
- **Appointment scheduling** — book, reschedule, cancel, and track appointments on a day/week/month calendar view
- **User management** — create Staff and Doctor accounts, reset passwords, and deactivate users
- **Clinic settings** — configure working hours, open days, and clinic logo
- **Account settings** — each user can set their own profile picture, display language, and color theme

### Who it is for

The system has three roles:

| Role | Access |
|---|---|
| **Admin** | Full access — manages users, clinic settings, patients, doctors, and appointments |
| **Staff** | Manages patients and appointments |
| **Doctor** | Views their own appointment calendar |

Patients are records only — they do not have login accounts.

---

## Tech stack

**Backend**
- .NET 8 / C# 12 — Clean Architecture (Domain → Application → Infrastructure → API)
- CQRS with MediatR + FluentValidation
- Entity Framework Core 8 + PostgreSQL 16
- OpenIddict 5 — OAuth 2.0 password flow, JWT tokens
- ASP.NET Core Identity

**Frontend**
- React 18 + TypeScript, Vite
- Tailwind CSS + Shadcn/UI components
- FullCalendar — interactive appointment calendar
- React Hook Form + Yup validation
- react-i18next — English, Brazilian Portuguese, and Spanish

**Infrastructure**
- Docker + Docker Compose
- nginx reverse proxy (production)

---

## Quick start (Docker)

The fastest way to run CliniSys is with Docker Compose. You need Docker Engine 24+ with Compose v2.

```bash
git clone https://github.com/willianbrecher/clinisys.git
cd clinisys

cp .env.example .env
# Open .env and set a strong POSTGRES_PASSWORD

docker compose up -d --build
```

Once the containers are healthy:

- **App** → http://localhost:3000
- **API** → http://localhost:8080
- **Swagger** → http://localhost:8080/swagger

Default admin credentials: `admin@clinisys.local` / `Admin@12345`
**Change the admin password after first login.**

To stop:
```bash
docker compose down

# Also remove the database volume (deletes all data):
docker compose down -v
```

---

## Development setup (hot reload)

For local development with live reload on both the API and the frontend.

**Prerequisites:** .NET 8 SDK, Node.js 20+, Docker

### 1. Start PostgreSQL

```bash
cp .env.example .env
docker compose -f docker-compose.dev.yml up -d postgres
```

### 2. Start the API

```bash
cd backend/src/CliniSys.Api
dotnet run
```

API: http://localhost:5110 · Swagger: http://localhost:5110/swagger

The API auto-applies migrations and seeds the admin account on first run.

### 3. Start the frontend

```bash
cd frontend
npm install
npm run dev
```

App: http://localhost:5173

The Vite dev server proxies `/api` and `/connect` to the backend automatically. If your API runs on a different port, set `VITE_BACKEND_URL` in `.env` (e.g. `VITE_BACKEND_URL=http://localhost:5000`).

---

## Environment variables

Copy `.env.example` to `.env` and adjust as needed:

| Variable | Description | Default |
|---|---|---|
| `POSTGRES_USER` | Database user | `clinisys` |
| `POSTGRES_PASSWORD` | Database password | `changeme` |
| `POSTGRES_DB` | Database name | `clinisys` |
| `AUTH_DISABLE_TRANSPORT_SECURITY` | Set `true` behind an HTTP-only reverse proxy | `false` |
| `VITE_BACKEND_URL` | Backend URL for the Vite dev proxy | `http://localhost:5110` |

---

## Project structure

```
clinisys/
├── backend/
│   └── src/
│       ├── CliniSys.Domain/          # Entities and enums
│       ├── CliniSys.Application/     # CQRS handlers, validators, interfaces
│       ├── CliniSys.Infrastructure/  # EF Core, repositories, Identity, seeds
│       └── CliniSys.Api/             # Controllers, middleware, Program.cs
└── frontend/
    └── src/
        ├── api/                      # Axios clients and typed API calls
        ├── auth/                     # Auth context and protected routes
        ├── features/                 # One folder per page/feature
        │   ├── appointments/
        │   ├── patients/
        │   ├── doctors/
        │   ├── users/
        │   ├── settings/
        │   ├── dashboard/
        │   └── account/
        └── components/               # Shared layout and UI components
```

---

## Seeding and extending

The seed pipeline runs on every startup and is idempotent. To add your own seed data:

1. Create a class implementing `IDataSeeder` in `CliniSys.Infrastructure/Persistence/Seeds/`
2. Set an `Order` value (higher runs later)
3. Register it: `services.AddScoped<IDataSeeder, YourSeeder>()`

The `AdminUserSeeder` (Order 1) is the built-in example.

---

## License

Apache License 2.0 — see [LICENSE](LICENSE).