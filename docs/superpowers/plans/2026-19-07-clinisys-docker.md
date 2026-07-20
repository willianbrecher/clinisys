# CliniSys Docker / Dev Environment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire up a development workflow (PostgreSQL in Docker, API + frontend native) and a production-ready docker-compose stack with multi-stage Dockerfiles for the .NET API and React frontend behind nginx.

**Architecture:** Monorepo root with `backend/` and `frontend/` subdirectories. Development: `docker-compose up postgres` only — API and frontend run natively for fast hot-reload. Production: single `docker-compose up` starts postgres, api, and frontend (nginx static).

**Tech Stack:** Docker Engine 24+, Docker Compose v2, .NET 8 SDK, Node 20, nginx 1.25-alpine, PostgreSQL 16-alpine.

## Global Constraints

- Docker Compose file format v3.8+
- Backend image: multi-stage `mcr.microsoft.com/dotnet/sdk:8.0-alpine` → `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`
- Frontend image: multi-stage `node:20-alpine` → `nginx:1.25-alpine`
- All secrets in `.env` (never committed); `.env.example` committed with placeholder values
- API listens on port 5000 internally; exposed as 5000 in dev, 8080 in prod compose
- Frontend nginx serves on port 80 internally; exposed as 3000 in prod compose
- `ASPNETCORE_ENVIRONMENT=Development` in dev; `Production` in prod image
- Connection string uses `Host=postgres` inside compose network, `Host=localhost` for native dev

---

### Task 1: Monorepo Root Scaffold + .gitignore

**Files:**
- Create: `.gitignore` (root)
- Create: `.env.example`
- Create: `.env` (local, never committed)

**Interfaces:**
- Produces: clean git root with all secrets and build artifacts excluded

- [ ] **Step 1: Create root `.gitignore`**

Create `.gitignore` at the repository root:
```gitignore
# .NET
backend/src/**/bin/
backend/src/**/obj/
backend/src/**/*.user
*.suo
.vs/

# Frontend
frontend/node_modules/
frontend/dist/
frontend/.vite/

# Environment secrets
.env
*.env.local

# Docker volumes
postgres-data/

# OS
.DS_Store
Thumbs.db
```

- [ ] **Step 2: Create `.env.example`**

```env
# Copy this file to .env and fill in values
POSTGRES_USER=clinisys
POSTGRES_PASSWORD=changeme
POSTGRES_DB=clinisys
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=clinisys;Username=clinisys;Password=changeme
ASPNETCORE_ENVIRONMENT=Development
```

- [ ] **Step 3: Create local `.env`**

```env
POSTGRES_USER=clinisys
POSTGRES_PASSWORD=clinisys_dev
POSTGRES_DB=clinisys
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=clinisys;Username=clinisys;Password=clinisys_dev
ASPNETCORE_ENVIRONMENT=Development
```

- [ ] **Step 4: Verify `.env` is git-ignored**

```bash
git status
```

Expected: `.env` does NOT appear in the output. If it does, the `.gitignore` pattern is wrong — fix it before proceeding.

- [ ] **Step 5: Commit**

```bash
git add .gitignore .env.example
git commit -m "chore: add root .gitignore and .env.example"
```

---

### Task 2: Development Docker Compose (PostgreSQL only)

**Files:**
- Create: `docker-compose.dev.yml`

**Interfaces:**
- Produces: `docker-compose -f docker-compose.dev.yml up -d postgres` starts a ready PostgreSQL instance for native API development

- [ ] **Step 1: Create `docker-compose.dev.yml`**

```yaml
version: "3.8"

services:
  postgres:
    image: postgres:16-alpine
    restart: unless-stopped
    env_file: .env
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 5s
      timeout: 5s
      retries: 10

volumes:
  postgres-data:
```

- [ ] **Step 2: Start postgres and verify**

```bash
docker compose -f docker-compose.dev.yml up -d postgres
```

Wait for healthy status:
```bash
docker compose -f docker-compose.dev.yml ps
```

Expected: `postgres` shows `healthy` (may take ~10 seconds on first start while the volume initialises).

- [ ] **Step 3: Verify connection from host**

```bash
docker exec -it $(docker compose -f docker-compose.dev.yml ps -q postgres) psql -U clinisys -d clinisys -c "\l"
```

Expected: psql connects and lists `clinisys` database.

- [ ] **Step 4: Update backend connection string for native dev**

In `backend/src/CliniSys.Api/appsettings.Development.json`, set:
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=clinisys;Username=clinisys;Password=clinisys_dev"
  }
}
```

This matches the `.env` values used by the dev compose service.

- [ ] **Step 5: Verify API starts and migrates**

```bash
cd backend/src/CliniSys.Api && dotnet run
```

Expected: migrations apply, seed admin user created, API starts on `http://localhost:5000`.

- [ ] **Step 6: Commit**

```bash
git add docker-compose.dev.yml backend/src/CliniSys.Api/appsettings.Development.json
git commit -m "chore: add dev docker-compose (PostgreSQL only) for native API development"
```

---

### Task 3: Backend Dockerfile (Multi-Stage)

**Files:**
- Create: `backend/Dockerfile`
- Create: `backend/.dockerignore`

**Interfaces:**
- Produces: `docker build -t clinisys-api backend/` produces a minimal aspnet runtime image

- [ ] **Step 1: Create `backend/.dockerignore`**

```dockerignore
**/bin/
**/obj/
**/*.user
.vs/
.git/
```

- [ ] **Step 2: Create `backend/Dockerfile`**

```dockerfile
# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Restore dependencies (cached layer)
COPY src/CliniSys.Domain/CliniSys.Domain.csproj          src/CliniSys.Domain/
COPY src/CliniSys.Application/CliniSys.Application.csproj src/CliniSys.Application/
COPY src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj src/CliniSys.Infrastructure/
COPY src/CliniSys.Api/CliniSys.Api.csproj                 src/CliniSys.Api/
RUN dotnet restore src/CliniSys.Api/CliniSys.Api.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/CliniSys.Api/CliniSys.Api.csproj \
    -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "CliniSys.Api.dll"]
```

- [ ] **Step 3: Build and verify image**

```bash
docker build -t clinisys-api:dev backend/
```

Expected: build succeeds, image size under 200 MB (alpine base).

```bash
docker images clinisys-api:dev
```

Expected: image listed with a reasonable size.

- [ ] **Step 4: Commit**

```bash
git add backend/Dockerfile backend/.dockerignore
git commit -m "chore: add multi-stage backend Dockerfile (SDK build → aspnet runtime)"
```

---

### Task 4: Frontend Dockerfile + nginx Config (Multi-Stage)

**Files:**
- Create: `frontend/Dockerfile`
- Create: `frontend/.dockerignore`
- Create: `frontend/nginx.conf`

**Interfaces:**
- Produces: `docker build -t clinisys-frontend frontend/` produces an nginx image serving the built SPA; all `/api` and `/connect` paths proxied to the API service

- [ ] **Step 1: Create `frontend/.dockerignore`**

```dockerignore
node_modules/
dist/
.vite/
.env*
```

- [ ] **Step 2: Create `frontend/nginx.conf`**

```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    # Proxy API requests to the backend service
    location /api/ {
        proxy_pass         http://api:5000;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }

    # Proxy OpenIddict token endpoint
    location /connect/ {
        proxy_pass         http://api:5000;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }

    # SPA fallback — all unknown routes → index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Compression
    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;
}
```

- [ ] **Step 3: Create `frontend/Dockerfile`**

```dockerfile
# --- Build stage ---
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies (cached layer)
COPY package.json package-lock.json ./
RUN npm ci

# Build SPA
COPY . .
RUN npm run build

# --- Runtime stage ---
FROM nginx:1.25-alpine AS runtime

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

- [ ] **Step 4: Build and verify image**

```bash
docker build -t clinisys-frontend:dev frontend/
```

Expected: build succeeds, image size under 50 MB (alpine nginx).

```bash
docker images clinisys-frontend:dev
```

Expected: image listed.

- [ ] **Step 5: Commit**

```bash
git add frontend/Dockerfile frontend/.dockerignore frontend/nginx.conf
git commit -m "chore: add multi-stage frontend Dockerfile and nginx reverse-proxy config"
```

---

### Task 5: Production Docker Compose

**Files:**
- Create: `docker-compose.yml`

**Interfaces:**
- Produces: `docker compose up -d` starts postgres, api, and frontend; all three services healthy; SPA reachable at `http://localhost:3000`; API reachable at `http://localhost:8080`

- [ ] **Step 1: Create `docker-compose.yml`**

```yaml
version: "3.8"

services:
  postgres:
    image: postgres:16-alpine
    restart: unless-stopped
    env_file: .env
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 5s
      timeout: 5s
      retries: 10
    networks:
      - clinisys

  api:
    build:
      context: backend
      dockerfile: Dockerfile
    restart: unless-stopped
    depends_on:
      postgres:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
    ports:
      - "8080:5000"
    networks:
      - clinisys

  frontend:
    build:
      context: frontend
      dockerfile: Dockerfile
    restart: unless-stopped
    depends_on:
      - api
    ports:
      - "3000:80"
    networks:
      - clinisys

volumes:
  postgres-data:

networks:
  clinisys:
    driver: bridge
```

- [ ] **Step 2: Update `appsettings.json` to read connection string from env**

In `backend/src/CliniSys.Api/appsettings.json`, ensure the connection string key matches the compose env var:
```json
{
  "ConnectionStrings": {
    "Default": ""
  }
}
```

The empty string is fine — `ConnectionStrings__Default` environment variable overrides it at runtime via ASP.NET Core's environment-variable configuration provider (double-underscore = `:` separator).

- [ ] **Step 3: Build and start all services**

```bash
docker compose up -d --build
```

This builds both images and starts all three services. Allow ~60 seconds for first-time image builds.

- [ ] **Step 4: Verify all services are healthy**

```bash
docker compose ps
```

Expected:
```
NAME                   STATUS          PORTS
clinisys-postgres-1    healthy         5432/tcp
clinisys-api-1         running         0.0.0.0:8080->5000/tcp
clinisys-frontend-1    running         0.0.0.0:3000->80/tcp
```

- [ ] **Step 5: Verify API is reachable**

```bash
curl -s http://localhost:8080/api/clinic-settings
```

Expected: HTTP 401 (unauthorized) — proves API is running and responding.

- [ ] **Step 6: Verify frontend is reachable**

Open `http://localhost:3000` in a browser. Expected: CliniSys login page loads. Log in with `admin@clinisys.local` / `Admin@12345` — dashboard renders.

- [ ] **Step 7: Verify API proxy through nginx**

Open browser DevTools → Network → log in. Verify: the POST to `/connect/token` and GET to `/api/clinic-settings` go to `http://localhost:3000/...` (nginx), not directly to port 8080.

- [ ] **Step 8: Commit**

```bash
git add docker-compose.yml backend/src/CliniSys.Api/appsettings.json
git commit -m "chore: add production docker-compose with postgres, api, and nginx frontend"
```

---

### Task 6: Developer Workflow Documentation (README)

**Files:**
- Create: `README.md`

**Interfaces:**
- Produces: single file with all commands a new developer needs to get CliniSys running

- [ ] **Step 1: Create `README.md`**

```markdown
# CliniSys

Single-clinic patient/doctor/appointment scheduling system.

## Prerequisites

- .NET 8 SDK
- Node.js 20+
- Docker Engine 24+ with Docker Compose v2

## Development (native hot-reload)

1. Copy environment file:
   ```bash
   cp .env.example .env
   # Edit .env if you want a different password
   ```

2. Start PostgreSQL:
   ```bash
   docker compose -f docker-compose.dev.yml up -d postgres
   ```

3. Start the API:
   ```bash
   cd backend/src/CliniSys.Api
   dotnet run
   # API available at http://localhost:5000
   # Swagger at http://localhost:5000/swagger
   ```

4. Start the frontend:
   ```bash
   cd frontend
   npm install
   npm run dev
   # App available at http://localhost:5173
   ```

5. Default credentials: `admin@clinisys.local` / `Admin@12345`

## Production (all containerized)

```bash
cp .env.example .env   # set strong POSTGRES_PASSWORD
docker compose up -d --build
# Frontend at http://localhost:3000
# API at http://localhost:8080
```

## Stopping

```bash
# Dev
docker compose -f docker-compose.dev.yml down

# Production
docker compose down
```

To remove persisted database data:
```bash
docker compose down -v
```
```

- [ ] **Step 2: Verify README commands work end-to-end**

Run through the Development section steps from scratch (with a fresh terminal) to confirm every command in the README is accurate.

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: add README with dev and production workflow"
```
