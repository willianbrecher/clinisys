# CliniSys — Dev Environment & Docker Design Spec

Date: 2026-07-19
Status: Approved

## 1. Purpose & Scope

Define the local development setup and the Dockerized production-like stack for
the CliniSys monorepo. Covers:

- PostgreSQL container for all environments
- Native `dotnet run` + `npm run dev` for fast hot-reload development
- Multi-stage Dockerfiles for the backend API and frontend SPA
- `docker-compose.yml` that brings up the full stack with one command

## 2. Monorepo Layout

```
clinisys/
├── backend/
│   ├── CliniSys.sln
│   ├── src/
│   │   ├── CliniSys.Domain/
│   │   ├── CliniSys.Application/
│   │   ├── CliniSys.Infrastructure/
│   │   └── CliniSys.Api/
│   └── Dockerfile
├── frontend/
│   ├── src/
│   ├── package.json
│   ├── vite.config.ts
│   └── Dockerfile
├── docker-compose.yml
└── .gitignore
```

## 3. Dev Workflow (Day-to-Day)

Start PostgreSQL only:
```bash
docker compose up -d postgres
```

Then run backend and frontend natively for hot reload:
```bash
# terminal 1
cd backend && dotnet run --project src/CliniSys.Api

# terminal 2
cd frontend && npm run dev
```

Backend reads `appsettings.Development.json` with the local Postgres connection.
Frontend dev server (`http://localhost:5173`) proxies `/api` and `/connect` to
`http://localhost:5000` (configured in `vite.config.ts`).

EF Core migrations: `cd backend && dotnet ef database update --project src/CliniSys.Infrastructure --startup-project src/CliniSys.Api`

## 4. Environment Variables

### Backend (set via `docker-compose.yml` for the container; `appsettings.Development.json` for native)

| Variable | Example value |
|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=postgres;Port=5432;Database=clinisys;Username=clinisys;Password=clinisys` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |

### Frontend (set via `docker-compose.yml`; `.env.development` for native)

| Variable | Example value |
|---|---|
| `VITE_API_BASE_URL` | `http://localhost:8080` (dev) / (empty = same origin in prod container) |

## 5. Backend Dockerfile

Multi-stage build using the official .NET 8 SDK and ASP.NET runtime images.

```dockerfile
# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CliniSys.sln .
COPY src/CliniSys.Domain/CliniSys.Domain.csproj           src/CliniSys.Domain/
COPY src/CliniSys.Application/CliniSys.Application.csproj src/CliniSys.Application/
COPY src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj src/CliniSys.Infrastructure/
COPY src/CliniSys.Api/CliniSys.Api.csproj                 src/CliniSys.Api/
RUN dotnet restore
COPY . .
RUN dotnet publish src/CliniSys.Api -c Release -o /app/publish --no-restore

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CliniSys.Api.dll"]
```

Placed at `backend/Dockerfile`. The container:
- Listens on `http://+:8080`
- Auto-applies EF Core migrations on startup (in `Program.cs`: `db.Database.MigrateAsync()`)
- Seeds the default admin user and `ClinicSettings` singleton if they don't exist

## 6. Frontend Dockerfile

Multi-stage build: Vite production bundle + nginx.

```dockerfile
# Stage 1: build
FROM node:20-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 2: serve
FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

`nginx.conf` at `frontend/nginx.conf`:
```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

Placed at `frontend/Dockerfile`. Served on port `80` inside the container,
mapped to `5173` on the host so the origin matches the native dev server
(backend CORS config needs no changes).

## 7. docker-compose.yml

```yaml
version: "3.9"

services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: clinisys
      POSTGRES_USER: clinisys
      POSTGRES_PASSWORD: clinisys
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U clinisys"]
      interval: 5s
      timeout: 5s
      retries: 5

  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: >-
        Host=postgres;Port=5432;Database=clinisys;
        Username=clinisys;Password=clinisys
    depends_on:
      postgres:
        condition: service_healthy

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "5173:80"
    depends_on:
      - backend

volumes:
  postgres_data:
```

## 8. .gitignore Additions

```
# .NET
backend/**/bin/
backend/**/obj/
backend/**/*.user

# Node
frontend/node_modules/
frontend/dist/

# Environment
.env
.env.local
backend/src/CliniSys.Api/appsettings.Production.json

# EF Core migrations snapshots are committed — do NOT ignore Migrations/
```

## 9. Startup Sequence (Containerized)

1. `postgres` starts; healthcheck waits until ready.
2. `backend` starts; `Program.cs` calls `db.Database.MigrateAsync()` and seed logic.
3. `frontend` starts; nginx serves the Vite bundle.
4. `frontend` JS makes API requests to `http://backend:8080` (set via
   `VITE_API_BASE_URL` build arg, baked into the bundle at build time).

Note: in the containerized stack the frontend calls `http://backend:8080` (Docker
internal hostname). In native dev the frontend proxies through Vite to
`http://localhost:5000`. The `VITE_API_BASE_URL` env var distinguishes the two.
