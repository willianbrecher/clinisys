# Configurable CORS Allowed Origins Implementation Plan

> Recorded retroactively — the work below already shipped. Steps reflect what was done.
> Spec: `docs/superpowers/specs/2026-08-27-configurable-cors-origins.md`.

**Goal:** Read the API's CORS allowed origins from configuration (comma-separated string) instead
of a hardcoded literal (#51). Spans backend + config → two PRs, both `Refs #51`.

**Tech Stack:** .NET 8 / C# 12, ASP.NET Core CORS, `docker-compose`.

## Global Constraints

- Branches `feature/51-configurable-cors-origins-*` referencing #51.
- Two-layer change → both PRs `Refs #51`; issue closed by hand after both merged (done
  2026-08-27, later re-closed in the 2026-08-29 doc-sync session's audit).
- `feature` label (repo default for new functionality).

---

### Task 1: Read `Cors:AllowedOrigins` from configuration — backend (#51)

**Branch:** `feature/51-configurable-cors-origins-backend` → PR #52 `Refs #51`

- [x] **Step 1:** In `Program.cs`, replace the literal `WithOrigins("http://localhost:5173")` with
  a value read from `builder.Configuration["Cors:AllowedOrigins"]`, split on `,` with
  `RemoveEmptyEntries | TrimEntries`.
- [x] **Step 2:** `appsettings.json` — add `"Cors": { "AllowedOrigins": "" }`.
- [x] **Step 3:** `appsettings.Development.json` — add
  `"Cors": { "AllowedOrigins": "http://localhost:5173" }`.
- [x] **Step 4:** `dotnet build`; verify a request from `http://localhost:5173` still gets CORS
  headers in Development and that clearing the setting removes them.
- [x] **Step 5:** Commit `feat: read CORS allowed origins from configuration`, open PR #52.

### Task 2: Expose the setting as a Docker env var — config (#51)

**Branch:** `feature/51-configurable-cors-origins-config` → PR #53 `Refs #51`

- [x] **Step 1:** `docker-compose.yml` `api.environment` — add
  `Cors__AllowedOrigins: ${CORS_ALLOWED_ORIGINS:-http://localhost:5173}`.
- [x] **Step 2:** `.env.example` — document `CORS_ALLOWED_ORIGINS` (comma-separated origins).
- [x] **Step 3:** Commit `docs: expose CORS allowed origins as a Docker env var`, open PR #53.
- [x] **Step 4:** After both merge, close #51.
