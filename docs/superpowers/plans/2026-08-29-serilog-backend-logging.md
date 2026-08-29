# Serilog Backend Logging Implementation Plan

> Implement task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the default console logging provider on the API with Serilog (#14) —
config-driven from `appsettings.json`, structured (compact JSON) output, one structured summary
line per HTTP request, **and a rolling log file whose path is configurable** (appsettings default
+ Docker env var). No change to any `ILogger<T>` consumer. Spec:
`docs/superpowers/specs/2026-08-29-serilog-backend-logging.md`.

**Tech Stack:** .NET 8/C# 12, `Serilog.AspNetCore` 8.x. Spans backend + docs layers → two PRs.

## Global Constraints

- Follow root `CLAUDE.md`: branches `feature/<slug>` referencing issue #14.
- Two-layer change → **both PRs use `Refs #14`**, not `Closes`. Close #14 by hand once both merge.
- Issue is labeled `enhancement`; repo has no `feature` label — use `enhancement`.
- No new comments unless they explain a non-obvious WHY.
- Per-layer PRs go to `master` (no long-lived feature branch for this).

---

### Task 1: Adopt Serilog as the API logging provider — backend (#14)

**Branch:** `feature/serilog-backend-logging` → PR `Refs #14`

**Files:**
- Modify: `backend/src/CliniSys.Api/CliniSys.Api.csproj`
- Modify: `backend/src/CliniSys.Api/Program.cs`
- Modify: `backend/src/CliniSys.Api/appsettings.json`
- Modify: `backend/src/CliniSys.Api/appsettings.Development.json`
- Modify: `.gitignore`

- [ ] **Step 1: Add the `Serilog.AspNetCore` package**

In `backend/src/CliniSys.Api/CliniSys.Api.csproj`, add to the `PackageReference` `ItemGroup`
(next to `Swashbuckle.AspNetCore`):

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />
```

Run `dotnet restore` from `backend/`. Confirm `Serilog.Sinks.File` and `Serilog.Sinks.Console`
resolve transitively (`dotnet list package --include-transitive`) — no separate sink package
needed.

- [ ] **Step 2: Wire Serilog into `Program.cs`**

`backend/src/CliniSys.Api/Program.cs`:

1. Add `using Serilog;` to the using block.
2. Before `var builder = WebApplication.CreateBuilder(args);`, add the bootstrap logger:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
```

3. Wrap the body — from `var builder = WebApplication.CreateBuilder(args);` through `app.Run();` —
   in `try { … } catch (Exception ex) { Log.Fatal(ex, "Application terminated unexpectedly."); } finally { Log.CloseAndFlush(); }`.
4. Right after the `connectionString` guard, register Serilog as the host logger:

```csharp
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());
```

5. Add `app.UseSerilogRequestLogging();` on the line after
   `app.UseMiddleware<LocalizationMiddleware>();`.

Resulting shape (unchanged lines elided):

```csharp
using System.Reflection;
using CliniSys.Api.Middleware;
using CliniSys.Application;
using CliniSys.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(connectionString, builder.Configuration);
    // ... AddControllers / Swagger / CORS unchanged ...

    var app = builder.Build();

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<LocalizationMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CliniSys.Infrastructure.Persistence.AppDbContext>();
        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<CliniSys.Infrastructure.Persistence.Seeds.DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
```

- [ ] **Step 3: `Serilog` section in `appsettings.json`**

Replace the `Logging` block with the `Serilog` block from spec §3.7 — `MinimumLevel` (Default
`Information`, overrides for `Microsoft.AspNetCore` and `Microsoft.EntityFrameworkCore` at
`Warning`), `Enrich: ["FromLogContext"]`, and `WriteTo` in **object form** with `console` and
`file` keys, both using `CompactJsonFormatter`. File args: `path: "logs/clinisys-.log"`,
`rollingInterval: "Day"`, `retainedFileCountLimit: 14`, `shared: true`.

Keep `AllowedHosts`, `ConnectionStrings`, `Cors` untouched.

- [ ] **Step 4: Development override in `appsettings.Development.json`**

Replace the `"Logging": { … }` line with the `Serilog` block from spec §3.7 — `MinimumLevel`
Default `Debug` / framework overrides at `Information`, and `WriteTo.console` only (readable
`outputTemplate`). Do **not** repeat the `file` key — it's inherited from the base.

- [ ] **Step 5: `.gitignore`**

Add:

```gitignore
# Logs
backend/src/CliniSys.Api/logs/
```

- [ ] **Step 6: Build and manually verify**

- `dotnet build` from `backend/` — clean.
- `dotnet run` from `backend/src/CliniSys.Api` (Development):
  - startup / migration / seed lines render through Serilog's text template;
  - `backend/src/CliniSys.Api/logs/clinisys-<date>.log` is created and contains compact-JSON lines;
  - one `HTTP GET /… responded 200 in … ms` line per request (hit Swagger or `GET /api/account/me`),
    in both console and file;
  - EF SQL visible at `Information`, no raw parameter dump.
- Force a 500 (stop Postgres mid-request) — `ExceptionMiddleware`'s `"Unhandled exception."` still
  logs, unchanged code, in both sinks.
- `ASPNETCORE_ENVIRONMENT=Production dotnet run` (reachable DB) — console output is compact JSON.
- `Serilog__WriteTo__file__Args__path=/tmp/clinisys-test-.log dotnet run` — file appears at that
  path instead, proving the env override key.
- Break the connection string and start — a `Fatal` line is logged and flushed before exit.

- [ ] **Step 7: Commit**

```bash
git add backend/src/CliniSys.Api/CliniSys.Api.csproj \
        backend/src/CliniSys.Api/Program.cs \
        backend/src/CliniSys.Api/appsettings.json \
        backend/src/CliniSys.Api/appsettings.Development.json \
        .gitignore
git commit -m "feat: adopt Serilog for backend logging with a rolling file sink"
```

- [ ] **Step 8: Open backend PR**

```bash
gh pr create --title "feat: adopt Serilog for backend logging with a rolling file sink" \
  --body "Refs #14

Replaces the default \`Microsoft.Extensions.Logging\` console provider with Serilog, configured from the \`Serilog\` section of the appsettings files (\`ReadFrom.Configuration\`). Sinks: console + a daily rolling file (\`logs/clinisys-<date>.log\`, 14 files retained), both compact-JSON in Production; Development keeps a readable console template. \`WriteTo\` is in object form so the file path is overridable via \`Serilog__WriteTo__file__Args__path\` (the Docker wiring for that lands in the docs PR). Adds \`UseSerilogRequestLogging\` for one structured summary line per request, with \`Microsoft.AspNetCore\`/\`Microsoft.EntityFrameworkCore\` turned down so it stays useful. Bootstrap logger + try/catch/finally around host build so a startup or migration failure is logged as \`Fatal\` and flushed.

No change to any \`ILogger<T>\` consumer (\`ExceptionMiddleware\` etc.) — they bridge through Serilog unchanged.

Spec: \`docs/superpowers/specs/2026-08-29-serilog-backend-logging.md\`" \
  --label enhancement --assignee willianbrecher
```

---

### Task 2: Make the container log path writable, persistent, configurable — docs (#14)

**Branch:** `feature/serilog-log-file-path-config` → PR `Refs #14`

**Files:**
- Modify: `backend/Dockerfile`
- Modify: `docker-compose.yml`
- Modify: `.env.example`

Do this branch **after** Task 1's PR merges (it references the env-var key Task 1 introduces).

- [ ] **Step 1: `backend/Dockerfile` — writable log dir for `appuser`**

Change the user-creation line to also create and own `/app/logs`, before `USER appuser`:

```dockerfile
RUN addgroup -S appgroup && adduser -S appuser -G appgroup \
 && mkdir -p /app/logs && chown appuser:appgroup /app/logs
USER appuser
```

- [ ] **Step 2: `docker-compose.yml` — env var + named volume**

Under `services.api.environment`, add:

```yaml
      Serilog__WriteTo__file__Args__path: ${LOG_FILE_PATH:-/app/logs/clinisys-.log}
```

Add a `volumes` block to the `api` service:

```yaml
    volumes:
      - api-logs:/app/logs
```

Add `api-logs:` to the top-level `volumes:` map (next to `postgres-data:`).

- [ ] **Step 3: `.env.example`**

Add after the `CORS_ALLOWED_ORIGINS` line:

```dotenv
# Path (inside the container) for the rolling API log file; the date is inserted before ".log"
LOG_FILE_PATH=/app/logs/clinisys-.log
```

- [ ] **Step 4: Verify**

- `docker compose build api` — Dockerfile change builds.
- `docker compose up -d` — API starts as `appuser`, no permission error on the log file.
- `docker compose exec api ls -la /app/logs` — `clinisys-<date>.log` present, owned by `appuser`.
- `docker compose exec api cat /app/logs/clinisys-<date>.log` — compact-JSON lines.
- `docker volume inspect clinisys_api-logs` — volume exists.
- Set `LOG_FILE_PATH=/app/logs/custom-.log` in `.env`, recreate the `api` container — file name
  changes accordingly.
- `docker compose down && docker compose up -d` — previous log file still in the volume.

- [ ] **Step 5: Commit**

```bash
git add backend/Dockerfile docker-compose.yml .env.example
git commit -m "docs: make the Serilog file-log path a configurable Docker env var"
```

- [ ] **Step 6: Open docs PR**

```bash
gh pr create --title "docs: make the Serilog file-log path a configurable Docker env var" \
  --body "Refs #14

Follow-up to the backend Serilog PR. Makes the containerised log file work: \`backend/Dockerfile\` creates \`/app/logs\` owned by the non-root \`appuser\`; \`docker-compose.yml\` mounts a named \`api-logs\` volume there so files survive \`docker compose down\`, and passes the path as \`Serilog__WriteTo__file__Args__path\` defaulted from a curated \`LOG_FILE_PATH\` env var (documented in \`.env.example\`).

Spec: \`docs/superpowers/specs/2026-08-29-serilog-backend-logging.md\`" \
  --label enhancement --assignee willianbrecher
```

- [ ] **Step 7: Close #14 manually** once both PRs have merged.
