# CliniSys — Serilog Backend Logging Spec

Date: 2026-08-29
Status: Draft
Issue: [#14](https://github.com/willianbrecher/clinisys/issues/14)

## 1. Goal

Replace the default `Microsoft.Extensions.Logging` console provider on the API with Serilog,
configured from `appsettings.json`, producing structured log output, a single structured summary
line per HTTP request, and **a rolling log file whose location is configurable** (an
`appsettings.json` default, overridable per environment and via a Docker env var).

The logging *abstraction* stays untouched — every `ILogger<T>` injection (e.g.
`ExceptionMiddleware`) keeps working through Serilog's `Microsoft.Extensions.Logging` bridge with
no code change. Only the provider and its configuration change.

## 2. Current behavior — confirmed

`backend/src/CliniSys.Api/Program.cs` never touches logging — it relies on the host default from
`WebApplication.CreateBuilder(args)`, which reads the `Logging` section of the appsettings files
and writes plain text to the console.

`appsettings.json:5-10`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

`appsettings.Development.json:11`:

```json
"Logging": { "LogLevel": { "Default": "Debug", "Microsoft.AspNetCore": "Information" } }
```

Consumers of the abstraction today:

- `ExceptionMiddleware` (`backend/src/CliniSys.Api/Middleware/ExceptionMiddleware.cs:39`) —
  `_logger.LogError(ex, "Unhandled exception.")` for anything that isn't a mapped
  `ValidationException`/`NotFoundException`/`ConflictException`.
- Framework logs (Kestrel, routing, EF Core command logging at `Debug` in Development, the
  OpenIddict/Identity pipeline).

The startup block in `Program.cs:60-67` (EF `MigrateAsync` + seed pipeline) runs before
`app.Run()` and currently logs migration/seed progress through the same default provider; a
failure there surfaces as an unhandled exception with a default host log line.

There is no `appsettings.Production.json`; the Docker `api` service runs with
`ASPNETCORE_ENVIRONMENT=Production`, so it uses `appsettings.json` only.

Container facts relevant to a file sink (`backend/Dockerfile`):

- Runtime image `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`, `WORKDIR /app`.
- A non-root user is created and activated (`RUN addgroup … adduser … USER appuser`) **before**
  `COPY --from=build /app/publish .`. `appuser` can only write where it's been given ownership —
  today, nowhere under `/app` is guaranteed writable by it.
- `docker-compose.yml` `api` service has no volumes; the only persisted volume in the repo is
  `postgres-data`.

## 3. Proposed design

### 3.1 Package

Add one package to `backend/src/CliniSys.Api/CliniSys.Api.csproj`:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />
```

`Serilog.AspNetCore` 8.x transitively brings everything this spec needs: `Serilog`,
`Serilog.Extensions.Hosting`, `Serilog.Settings.Configuration` (the `ReadFrom.Configuration`
binder), `Serilog.Sinks.Console`, **`Serilog.Sinks.File`**, `Serilog.Formatting.Compact` (the
`CompactJsonFormatter`), and the `UseSerilogRequestLogging` middleware. No other Serilog package
is required.

### 3.2 Sinks — decision

**Console + rolling file**, both declared in configuration:

- **Console** — container stdout / the local dev terminal, where logs are already looked for.
- **File** — a rolling daily file for retained history. `rollingInterval: Day`,
  `retainedFileCountLimit: 14` (two weeks), `shared: true` (safe if the process is ever run
  multi-instance on one host), compact-JSON formatter so the file is machine-parseable.

An external sink (Seq/Elasticsearch) stays out of scope — that's a deployment choice, and the
`WriteTo` config makes adding one later a config + package change with no `Program.cs` edit.

### 3.3 File path — configurable, decision

`WriteTo` is written in **object form** (named keys) rather than an array, so an environment
variable can target a **stable key** instead of a fragile positional index:

```json
"WriteTo": {
  "console": { "...": "..." },
  "file":    { "Name": "File", "Args": { "path": "logs/clinisys-.log", "...": "..." } }
}
```

Override precedence for the path, low → high:

| Layer | Value | Set where |
|---|---|---|
| Base default | `logs/clinisys-.log` (relative to the content root) | `appsettings.json` |
| Local dev | same — the relative `logs/` dir, git-ignored | inherited; no override |
| Docker | `/app/logs/clinisys-.log` | `docker-compose.yml` env var (see 3.7) |
| Any host | anything | `Serilog__WriteTo__file__Args__path` env var |

With `rollingInterval: Day`, Serilog inserts the date before the extension —
`logs/clinisys-20260829.log`, `clinisys-20260830.log`, … The directory in `path` is created by
the sink if missing (but the parent must be writable — see 3.7 for the container).

### 3.4 Output format — decision

- **Development** (`appsettings.Development.json`): console uses a readable `outputTemplate`
  (timestamp, level, source context, message, exception). The file sink keeps compact JSON.
- **Production** (`appsettings.json`): both console and file use
  `Serilog.Formatting.Compact.CompactJsonFormatter` — one JSON object per event, so a collector
  parses `RequestPath`, `StatusCode`, `Elapsed`, `SourceContext`, exception details without
  regex. This is the "structured logging output" the issue asks for.

### 3.5 Request logging — decision

**Yes** — add `app.UseSerilogRequestLogging()` immediately after the two existing custom
middlewares. It collapses the several framework log lines emitted per request into one
`HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed} ms` event with those
values as structured properties. To keep that summary useful, the framework's own
request-pipeline categories are turned down to `Warning` via `MinimumLevel.Override` (see 3.6).

### 3.6 `Program.cs` wiring

Serilog's documented two-stage pattern — a bootstrap logger created before the host is built so
that failures during configuration/DI/migration are logged through Serilog too, then the real
logger reconfigured from `IConfiguration` + DI once the host exists:

```csharp
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

    // ... existing service registration unchanged ...

    var app = builder.Build();

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<LocalizationMiddleware>();
    app.UseSerilogRequestLogging();

    // ... existing pipeline unchanged ...

    using (var scope = app.Services.CreateScope())
    {
        // ... existing migrate + seed block unchanged ...
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

Notes:

- `WebApplication.CreateBuilder(args)` moves inside `try` so a startup crash (bad connection
  string, migration failure) is logged as `Fatal` and the sinks are flushed before exit.
- `ReadFrom.Services(services)` lets any `ILogEventEnricher` registered in DI participate; none is
  today, but it's the standard form and costs nothing.
- The `Logging` section in both appsettings files is **removed** — Serilog ignores it, and leaving
  it invites someone to edit the wrong knob. Levels move to the `Serilog` section (3.7).
- No change to `ExceptionMiddleware` or any other `ILogger<T>` consumer.

### 3.7 Configuration

`appsettings.json` — replace the `Logging` section with:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "Enrich": [ "FromLogContext" ],
  "WriteTo": {
    "console": {
      "Name": "Console",
      "Args": {
        "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
      }
    },
    "file": {
      "Name": "File",
      "Args": {
        "path": "logs/clinisys-.log",
        "rollingInterval": "Day",
        "retainedFileCountLimit": 14,
        "shared": true,
        "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
      }
    }
  }
}
```

`appsettings.Development.json` — replace the `Logging` line with an override that lowers levels and
switches the **console** back to human-readable text (the `file` key is not repeated, so it's
inherited from the base unchanged):

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Debug",
    "Override": {
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "WriteTo": {
    "console": {
      "Name": "Console",
      "Args": {
        "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}  {Message:lj}{NewLine}{Exception}"
      }
    }
  }
}
```

`Serilog.Settings.Configuration` merges the two `Serilog` sections by key — the Development
`console` entry replaces the base `console`, `file` and `Enrich` carry through. Same key-merge
behaviour is what makes `Serilog__WriteTo__file__Args__path` a reliable override target.

`Microsoft.EntityFrameworkCore` is pinned to `Warning` in production (it was implicitly
`Information` before via `Default`) so EF's per-command SQL logging doesn't flood production; in
Development it's raised to `Information` — the same effective verbosity as today's
`"Default": "Debug"` gave, minus the raw parameter values EF logs at `Debug`.

Local (non-Docker) `dotnet run` writes to `backend/src/CliniSys.Api/logs/` — added to
`.gitignore`.

### 3.8 Docker (docs-layer PR)

`backend/Dockerfile` — create the log directory and hand it to `appuser` **before** the
`USER appuser` line:

```dockerfile
RUN addgroup -S appgroup && adduser -S appuser -G appgroup \
 && mkdir -p /app/logs && chown appuser:appgroup /app/logs
USER appuser
```

`docker-compose.yml` `api` service — pass the path as an env var (curated name, defaulted) and
mount a named volume so the files survive `docker compose down`:

```yaml
    environment:
      # ... existing ...
      Serilog__WriteTo__file__Args__path: ${LOG_FILE_PATH:-/app/logs/clinisys-.log}
    volumes:
      - api-logs:/app/logs
```

```yaml
volumes:
  postgres-data:
  api-logs:
```

`.env.example` — document the knob:

```dotenv
# Path (inside the container) for the rolling API log file; the date is inserted before ".log"
LOG_FILE_PATH=/app/logs/clinisys-.log
```

`.gitignore` — ignore the local dev log directory:

```gitignore
# Logs
backend/src/CliniSys.Api/logs/
```

## 4. PR split

Per root `CLAUDE.md`, this spans two layers → two PRs, both `Refs #14` (close #14 by hand once
both merge):

1. **Backend PR** — `CliniSys.Api.csproj`, `Program.cs`, `appsettings.json`,
   `appsettings.Development.json`, `.gitignore` (the `logs/` entry lives with the code that
   creates it).
2. **Docs PR** — `backend/Dockerfile`, `docker-compose.yml`, `.env.example`.

The backend PR works standalone (file path just defaults to the relative `logs/` dir); the docs
PR makes the containerised path writable, persistent, and configurable.

## 5. Non-goals

- **No Seq / Elasticsearch / other network sink.** Console + rolling file only.
- **No `LOG_LEVEL` env knob.** Only the file *path* is exposed as a curated Docker variable;
  changing levels is a `Serilog__MinimumLevel__*` env var or an `appsettings` edit — add a
  curated name only if a real need appears.
- **No log shipping / rotation beyond `retainedFileCountLimit`.** 14 daily files, then Serilog
  deletes the oldest. Disk-space monitoring is an ops concern, not app config.
- **No structured-logging rewrite of call sites.** `ExceptionMiddleware` and future `ILogger<T>`
  usage keep the standard message-template form; this issue is about the provider.
- **No `appsettings.Production.json`.** Production config stays in `appsettings.json`.
- **No frontend change.**
