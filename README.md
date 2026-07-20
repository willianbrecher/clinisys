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
