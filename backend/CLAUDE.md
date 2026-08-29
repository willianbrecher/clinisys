# CLAUDE.md — backend

Guidance for Claude Code when working under `backend/`. See the root `CLAUDE.md` for repo-wide
PR/branch/comment conventions that apply here too.

## What this is

The CliniSys API: .NET 8 / C# 12, Clean Architecture (Domain → Application → Infrastructure →
API), CQRS via MediatR, EF Core 8 + PostgreSQL 16, OpenIddict 5 (OAuth2 password flow, JWT) on
top of ASP.NET Core Identity.

Three roles enforced across the API: **Admin** (full access), **Staff** (patients + appointments),
**Doctor** (own calendar only). Patients are records only, not login accounts.

## Commands

Run from `backend/src/CliniSys.Api`:

```bash
dotnet run                  # run API with hot reload (auto-applies EF migrations + seeds admin on startup)
```

Run from `backend/`:

```bash
dotnet build                # build the solution
dotnet test                 # run the unit tests
```

## Tests

`test/CliniSys.Application.Tests` (NUnit 4) — the only test project. It covers the
`CliniSys.Application` layer: FluentValidation validators, MediatR command/query handlers, and
`ValidationBehaviour`. Handler dependencies (repository interfaces, `IIdentityService`) are
substituted with NSubstitute — **no database, no `WebApplicationFactory`**. Infrastructure
repositories and EF Core queries are not exercised.

Conventions:

- **Folders group by aggregate** (`Appointments/`, `Patients/`, …), one level deep — they do
  *not* mirror the production `Commands/<Verb>/` nesting. Namespaces follow the folder
  (`CliniSys.Application.Tests.Appointments`).
- One `[TestFixture]` per class under test, file named `<ClassUnderTest>Tests.cs`.
- Test methods named `Method_Scenario_ExpectedResult` (or a plain descriptive name for
  validators); Arrange/Act/Assert.
- Asserts via **FluentAssertions** (`result.Should().Be(...)`,
  `await act.Should().ThrowAsync<ConflictException>()`); validators use
  `FluentValidation.TestHelper` (`validator.TestValidate(cmd).ShouldHaveValidationErrorFor(...)`).
- Substitutes: `Substitute.For<IAppointmentRepository>()`, stub reads with `.Returns(...)`,
  assert writes with `.Received(1)` / `.DidNotReceive()`.
- `TestSupport/Builders.cs` — valid-by-default command/entity factories; each test overrides only
  the field it exercises.
- Package versions are pinned deliberately (see `CliniSys.Application.Tests.csproj`):
  FluentAssertions `< 8.0` (v8 is paid-license), NSubstitute over Moq (no build-time telemetry).

EF Core migrations live in `CliniSys.Infrastructure/Persistence/Migrations`. Add new ones from
`backend/src/CliniSys.Infrastructure`:

```bash
dotnet ef migrations add <Name> --startup-project ../CliniSys.Api
```

## Architecture — Clean Architecture + CQRS

Layers, dependency direction Domain ← Application ← Infrastructure ← Api:

- **`CliniSys.Domain`** — entities and enums only, no dependencies on other layers.
- **`CliniSys.Application`** — CQRS handlers via MediatR, FluentValidation validators, and the
  interfaces the outer layers implement (`Common/Interfaces`, including repository interfaces
  like `IPatientRepository`). Organized as `Commands/<Aggregate>/<Verb>/` and
  `Queries/<Aggregate>/<Verb>/`, each folder holding a `*Command(orQuery).cs`, `*Handler.cs`, and
  (for commands) a `*Validator.cs`. Commands implement `ICommand<T>`/handlers implement
  `ICommandHandler<TCommand, TResult>`; same pattern for queries. `Locales/*.json` hold localized
  validation/error message strings, resolved via `IMessageLocalizer`.
- **`CliniSys.Infrastructure`** — EF Core `AppDbContext`, repository implementations, ASP.NET Core
  Identity, OpenIddict setup, and the seed pipeline (`Persistence/Seeds`).
- **`CliniSys.Api`** — thin controllers, two global middlewares (`ExceptionMiddleware` maps
  `ValidationException`/`NotFoundException`/`ConflictException` to JSON error responses;
  `LocalizationMiddleware` sets thread culture from `Accept-Language`, falling back to `en-US`),
  and `Program.cs` composition root.

Auth: OpenIddict 5 OAuth2 password flow issuing JWTs, backed by ASP.NET Core Identity.
`ConnectController` handles the token endpoint.

**Seed pipeline**: every `IDataSeeder` implementation in `Infrastructure/Persistence/Seeds` runs
on startup (idempotent), ordered by an `Order` property. `AdminUserSeeder` (Order 1) seeds the
default admin and is the reference implementation for adding new seeders — register new ones with
`services.AddScoped<IDataSeeder, YourSeeder>()`.

## Notes

- Error responses are JSON `{ message, errors? }`, always produced through `ExceptionMiddleware` —
  throw `NotFoundException`/`ConflictException`/FluentValidation `ValidationException` from
  handlers rather than writing custom error responses in controllers.
- Supported locales are `en-US`, `pt-BR`, `es-ES`. When adding user-facing text, add keys to all
  three files in `CliniSys.Application/Locales`, and keep them in sync with the frontend's
  `src/locales/*` bundles.
- Default seeded admin: `admin@clinisys.local` / `Admin@12345` (change after first login in any
  real deployment).
- **Don't fetch a single record by reusing a paginated list query with a large `pageSize`.**
  `DoctorsController.GetById` used to call `GetDoctorsQuery(1, 1000)` and filter client-side for
  the matching ID — but the list handlers reject any `PageSize > 100`
  (`if (request.PageSize > 100) throw new ValidationException(...)`), so that call failed
  unconditionally, every time (fixed in `GetDoctorByIdQuery`/`GetDoctorByIdQueryHandler`, backed
  by `IDoctorRepository.GetByIdWithUserAsync`). `PatientsController.GetById` has the identical bug
  (`GetPatientsQuery(null, 1, 1000)` against the same cap in `GetPatientsQueryHandler`) and is
  still unfixed — give it the same dedicated-query treatment before touching patient editing.
