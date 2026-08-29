# CliniSys — Configurable CORS Allowed Origins Spec

Date: 2026-08-27
Status: Shipped — PRs [#52](https://github.com/willianbrecher/clinisys/pull/52) (backend),
[#53](https://github.com/willianbrecher/clinisys/pull/53) (config), merged 2026-08-27.
Recorded retroactively — the work shipped straight from the issue without a spec/plan.
Issue: [#51](https://github.com/willianbrecher/clinisys/issues/51)

## 1. Goal

Make the API's CORS allowed origins configurable instead of a single hardcoded literal, so a
deployed frontend URL can be added without editing and redeploying the backend.

## 2. Prior behavior

`backend/src/CliniSys.Api/Program.cs` registered the default CORS policy with a literal string:

```csharp
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
```

`http://localhost:5173` (Vite's dev port) was not read from `IConfiguration` at all — unlike
`Auth:DisableTransportSecurity`, which was already config-driven and Docker-overridable via the
`Auth__DisableTransportSecurity` double-underscore env var (`docker-compose.yml`).

## 3. What shipped

### 3.1 Backend (PR #52)

The issue flagged a design choice: .NET's env-var config binder needs indexed keys for arrays
(`Cors__AllowedOrigins__0`, `__1`, …), awkward for a single `docker-compose.yml` env var.
**Decision taken: a single comma-separated string, split in `Program.cs`** — not a JSON array.

`Program.cs`:

```csharp
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
```

Config defaults:

- `appsettings.json` — `"Cors": { "AllowedOrigins": "" }` (no origins allowed unless configured).
- `appsettings.Development.json` — `"Cors": { "AllowedOrigins": "http://localhost:5173" }`.

### 3.2 Docker / config (PR #53)

- `docker-compose.yml` `api` service:
  `Cors__AllowedOrigins: ${CORS_ALLOWED_ORIGINS:-http://localhost:5173}` — same single-value
  override style as `Auth__DisableTransportSecurity`.
- `.env.example`: documents `CORS_ALLOWED_ORIGINS` (comma-separated, e.g. the deployed frontend
  URL).

## 4. Non-goals

- No JSON-array form for the setting — the comma-separated string was chosen precisely to keep the
  Docker env var a single scalar. (Both `appsettings` files also use the string form, not an
  array, for consistency with the env var.)
- No per-endpoint or named CORS policies — the single default policy is unchanged in shape.
- `AllowAnyHeader()` / `AllowAnyMethod()` unchanged — only the origin list became configurable.

## 5. Follow-up

The `Serilog__WriteTo__file__Args__path` env var added later
([2026-08-29 Serilog spec](2026-08-29-serilog-backend-logging.md), §3.8) followed this same
"curated short env var → double-underscore config key, defaulted in `docker-compose.yml`"
pattern.
