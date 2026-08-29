# CliniSys — Backend Unit Tests (NUnit) Spec

Date: 2026-08-29
Status: Draft — pending review
Issue: [#60](https://github.com/willianbrecher/clinisys/issues/60)

## 1. Goal

Stand up a backend test project on **NUnit** and cover the `CliniSys.Application` layer — where
the app's actual logic lives: FluentValidation validators, MediatR command/query handlers, and
the validation pipeline behaviour. Add a GitHub Actions workflow that runs `dotnet test` on every
backend PR (the repo has no CI today).

Decisions already taken (issue #60 follow-up):

| Question | Decision |
|---|---|
| Framework | NUnit |
| First-pass scope | validators + handlers + domain/guardrail rules |
| Handler dependencies | substitute the repository interfaces (no real DB) |
| Support libraries | NSubstitute + FluentAssertions |
| CI | yes — GitHub Actions running `dotnet test` on PRs, in this batch |

## 2. Current state — confirmed

- `backend/CliniSys.sln` — 4 projects under a `src` solution folder (`Domain`, `Application`,
  `Infrastructure`, `Api`). No `test`/`tests` folder, no `*.Tests.csproj`.
- `backend/CLAUDE.md` says: *"There is no backend test project in this repo yet — don't assume
  `dotnet test` works until one is added."*
- No `.github/workflows/` — nothing runs on PRs.
- Logic worth covering, all in `CliniSys.Application`:
  - **Validators** — `src/CliniSys.Application/**/*Validator.cs`. Pure `AbstractValidator<T>`
    subclasses, no injected dependencies (checked — none take `IMessageLocalizer`; messages are
    literals or FluentValidation defaults). Cheapest to test.
  - **Handlers** — `*Handler.cs`, constructor-inject repository interfaces from
    `Common/Interfaces/Repositories` (`IAppointmentRepository`, `IPatientRepository`, …) plus
    `IClinicSettingsRepository`, `IUserRepository`, `IIdentityService`.
  - **Guardrail logic** lives *in the handlers*, not the domain entities — `Appointment` etc. are
    anemic data classes. Examples:
    - `CreateAppointmentCommandHandler` — `ValidateOpenHours` (open-days + open-hours →
      `ConflictException`), `CheckOverlap` (doctor double-booking, ignores `Cancelled`).
    - `UpdateAppointmentStatusCommandHandler` — an explicit transition matrix (`switch` on
      `(from, to)`), no-op allowed, everything else → `ConflictException`. This is exactly the
      logic issues #32/#33 churned on.
  - **Pipeline** — `Behaviours/ValidationBehaviour.cs` — runs all `IValidator<TRequest>`, throws
    `ValidationException` on failure, else calls `next`.
- Known bugs a test would have caught: #30 (single fetch via a `PageSize` over the 100 cap →
  always threw), #32 (no-op status transition rejected), #54 (naive-vs-offset `DateTime`).
- `CliniSys.Application.csproj`: `net8.0`, `Nullable` + `ImplicitUsings` enabled,
  `GenerateDocumentationFile` true. References `MediatR` 12.*, `FluentValidation
  .DependencyInjectionExtensions` 11.*.

## 3. Proposed design

### 3.1 Project layout

One test project targeting the Application layer:

```
backend/
├── CliniSys.sln                 # + a "test" solution folder, + the project below
├── src/  …
└── test/
    └── CliniSys.Application.Tests/
        ├── CliniSys.Application.Tests.csproj
        ├── Appointments/         # both Command and Query tests for the aggregate, flat
        │   ├── CreateAppointmentCommandValidatorTests.cs
        │   ├── CreateAppointmentCommandHandlerTests.cs
        │   ├── UpdateAppointmentStatusCommandHandlerTests.cs
        │   └── RescheduleAppointmentCommandHandlerTests.cs
        ├── Patients/             # validators + GetPatientById + GetPatients tests
        ├── Doctors/
        ├── HealthPlans/
        ├── Account/
        ├── Users/
        ├── Behaviours/
        │   └── ValidationBehaviourTests.cs
        └── TestSupport/
            ├── Builders.cs       # entity/command builders with sane defaults
            └── SubstituteExtensions.cs # small NSubstitute helpers if needed
```

- **One project, not one-per-layer.** `Domain` has no behaviour to test; `Infrastructure`
  repositories are substituted away, not exercised (handler-dependency decision); `Api` controllers
  are thin pass-throughs. If real-DB repository tests are wanted later, add
  `CliniSys.Infrastructure.Tests` then — out of scope here (§5).
- **Folder layout does not need to mirror production.** The test project references
  `CliniSys.Application.csproj`, so every `public` type (handlers, validators, models — all are
  `public`) is directly usable regardless of where the test file sits. Production nests
  `Commands/<Aggregate>/<Verb>/` only because each leaf folder bundles a Command + Handler +
  Validator; the test project has no such reason, so it stays one level deep: **one folder per
  aggregate**, Command and Query test fixtures side by side inside it.
- **Namespaces** follow the folder: `CliniSys.Application.Tests.Appointments`.
- **File naming:** `<ClassUnderTest>Tests.cs`, one `[TestFixture]` per production class.

### 3.2 `CliniSys.Application.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>12.0</LangVersion>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="NUnit" Version="4.*" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.*" />
    <PackageReference Include="NUnit.Analyzers" Version="4.*" />
    <PackageReference Include="NSubstitute" Version="5.*" />
    <PackageReference Include="NSubstitute.Analyzers.CSharp" Version="1.*" />
    <PackageReference Include="FluentAssertions" Version="[7.0.0,8.0.0)" />
    <PackageReference Include="FluentValidation" Version="11.*" />
    <PackageReference Include="coverlet.collector" Version="6.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CliniSys.Application\CliniSys.Application.csproj" />
  </ItemGroup>
</Project>
```

Version notes (the `FluentAssertions` range is worth a code comment in the csproj):

- `FluentAssertions` `[7.0.0,8.0.0)` — v8 (Jan 2025) switched to a paid commercial license.
  CliniSys is Apache-2.0; v7.x is the last free line.
- `NSubstitute` 5.x — MIT-licensed, no build-time telemetry (the reason it's preferred here over
  Moq, whose 4.20.0–4.20.69 range shipped the SponsorLink analyzer). `NSubstitute.Analyzers
  .CSharp` catches misuse (e.g. `Received()` on a non-substitute) at build time.
- `NUnit` 4.x — `Assert.That` only (the classic `Assert.AreEqual` model is gone in 4.x);
  `NUnit.Analyzers` enforces this.

### 3.3 Solution wiring

`dotnet sln backend/CliniSys.sln add backend/test/CliniSys.Application.Tests/CliniSys.Application.Tests.csproj --solution-folder test`

(the `.sln` already uses a `src` solution folder; add a sibling `test` folder).

### 3.4 What gets tested — first pass

**Validators** (`FluentValidation.TestHelper`, `validator.TestValidate(cmd)`):

| Validator | Cases |
|---|---|
| `CreateAppointmentCommandValidator` | `PatientId`/`DoctorId` empty → error; `StartsAt` in the past/now → error, future → ok; `DurationMinutes` 4/481 → error, 5/480/60 → ok |
| `RescheduleAppointmentCommandValidator` | same shape as above + `Id` empty |
| `CreatePatientCommandValidator` / `UpdatePatientCommandValidator` | required `FullName`, `DateOfBirth` bounds, `Email` format when present, `HealthPlanNumber` max length, `HealthPlanId` uuid-or-empty |
| `ChangePasswordCommandValidator` | `NewPassword` empty / too short / ok |
| `CreateUserCommandValidator` / `ResetPasswordCommandValidator` | required fields, password min length, role in allowed set |
| `CreateHealthPlanCommandValidator` / `UpdateHealthPlanCommandValidator` | `Name` required + max length |
| `UpdateClinicSettingsCommandValidator` | open/close time ordering, open-days format |

One `[Test]` per rule branch, or `[TestCase]` rows for numeric bounds.

**Handlers** (substitute the injected interfaces; assert result + `Received()` the writes):

- `CreateAppointmentCommandHandler`
  - clinic closed that weekday → `ConflictException`
  - start before `OpenTime` / end after `CloseTime` → `ConflictException`
  - overlapping non-cancelled appointment for the doctor → `ConflictException`
  - overlapping appointment that is `Cancelled` → ignored, succeeds
  - happy path → `AddAsync` + `SaveChangesAsync` called once, returns the new `Guid`
- `UpdateAppointmentStatusCommandHandler`
  - appointment missing → `NotFoundException`
  - every allowed transition in the matrix (incl. same→same no-op) → succeeds, `Update` +
    `SaveChangesAsync` called (`[TestCase]` matrix)
  - a rejected transition (e.g. `Completed`→`Scheduled`) → `ConflictException`, no save
- `RescheduleAppointmentCommandHandler` — open-hours + overlap guardrails, `excludeId` = the
  appointment being moved (so it doesn't conflict with itself)
- `GetPatientByIdQueryHandler` / `GetDoctorByIdQueryHandler`
  - found → mapped model (incl. `HealthPlanName` via the with-plan repo call for patients)
  - missing → `NotFoundException` (regression guard for #30 — assert it calls the dedicated
    `GetByIdWithHealthPlanAsync` / `GetByIdWithUserAsync`, never a paged list)
- `GetPatientsQueryHandler` / `GetDoctorsQueryHandler` / `GetHealthPlansQueryHandler`
  - `PageSize > 100` → `ValidationException` (the cap CLAUDE.md calls out)
  - search term forwarded to the repo; page/pageSize forwarded; items mapped
- `CreateHealthPlanCommandHandler` / `UpdateHealthPlanCommandHandler` /
  `DeactivateHealthPlanCommandHandler` — create returns id + saves; update mutates + saves;
  update/deactivate on missing id → `NotFoundException`; deactivate sets `IsActive = false`
- `UpdatePreferencesCommandHandler` + `GetCurrentUserPreferencesQueryHandler` (from #40) —
  round-trips theme/language through `IUserRepository`, null when the user is gone

**Pipeline** — `ValidationBehaviourTests`:

- no validators registered → `next` invoked once, its result returned
- all validators pass → `next` invoked
- one validator returns failures → `ValidationException` thrown carrying those failures, `next`
  **not** invoked

Uses a throwaway `record FakeRequest : IRequest<FakeResponse>`,
`Substitute.For<IValidator<FakeRequest>>()`, and a `RequestHandlerDelegate<FakeResponse>` spy.

**Domain** — nothing this pass. `Appointment`/`Patient`/etc. carry no methods; `CreatedAt =
DateTime.UtcNow` defaults aren't worth a test. Noted so a reviewer doesn't expect
`CliniSys.Domain.Tests`.

### 3.5 Test conventions (for `backend/CLAUDE.md`)

- `[TestFixture]` per production class; `[Test]` / `[TestCase]` methods named
  `Method_Scenario_ExpectedResult` (e.g.
  `Handle_WhenTransitionRejected_ThrowsConflictException`).
- Arrange/Act/Assert with a blank line between; asserts via FluentAssertions
  (`result.Should().Be(...)`, `act.Should().Throw<ConflictException>()`).
- Substitutes: `Substitute.For<IAppointmentRepository>()`; stub reads with `.Returns(...)` and
  assert the writes that matter with `.Received(1).AddAsync(...)` /
  `.DidNotReceive().SaveChangesAsync()`.
- `TestSupport/Builders.cs` — `Builders.Appointment(status: …, startsAt: …)`,
  `Builders.ClinicSettings(openDays: "1,2,3,4,5")`, `Builders.CreateAppointmentCommand(…)` with
  valid defaults so each test overrides only the field it's about.
- No shared mutable fixture state unless genuinely constant; prefer local arrange.

### 3.6 CI workflow (docs-layer PR)

`.github/workflows/backend-tests.yml` — the repo's first workflow:

```yaml
name: backend-tests

on:
  push:
    branches: [master]
  pull_request:
    paths:
      - 'backend/**'
      - '.github/workflows/backend-tests.yml'

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore backend/CliniSys.sln
      - run: dotnet build backend/CliniSys.sln --no-restore --configuration Release
      - run: >
          dotnet test backend/CliniSys.sln --no-build --configuration Release
          --verbosity normal --collect:"XPlat Code Coverage"
```

No coverage gate yet — collect it, don't fail on it. A threshold can come later once there's a
baseline.

### 3.7 Docs updates

- `backend/CLAUDE.md` — replace the "no test project" note with: the `dotnet test` command, the
  `test/CliniSys.Application.Tests` location, and §3.5's conventions.
- root `CLAUDE.md` — note that backend PRs now run `dotnet test` in CI and should keep it green;
  add `test/` to any structure description.

## 4. PR split

Per root `CLAUDE.md` (`backend/` vs. outside-both):

1. **Backend PR — scaffold + validators + pipeline.** `test/CliniSys.Application.Tests` project,
   solution wiring, `TestSupport/`, all validator fixtures, `ValidationBehaviourTests`,
   `backend/CLAUDE.md` update. `Refs #60`. Green `dotnet test` locally.
2. **Backend PR — handler fixtures.** All the handler tests from §3.4. `Refs #60`. Branch from
   master after PR 1 merges.
3. **Docs PR — CI + root docs.** `.github/workflows/backend-tests.yml`, root `CLAUDE.md`.
   `Refs #60`. Merge after PR 1 (so the first CI run has a test project to find).

All three `Refs #60`; close #60 by hand once all merge. If the reviewer would rather have one
backend PR, PRs 1+2 can be merged into one — noted in the plan.

## 5. Non-goals

- **No real-database tests.** Repository implementations, EF Core queries (`ILike`, `Include`,
  pagination SQL), and migrations are not exercised — handlers see substituted interfaces. A future
  `CliniSys.Infrastructure.Tests` (Testcontainers-Postgres or SQLite) is a separate issue.
- **No API / integration tests.** No `WebApplicationFactory`, no HTTP-level tests, no auth/
  OpenIddict flow tests.
- **No coverage threshold / gate.** CI collects coverage but doesn't enforce a number.
- **No frontend tests.** Separate concern, separate issue.
- **No test for the Serilog/CORS/config plumbing** in `Program.cs` — composition-root wiring,
  not unit-testable logic.
- **No backfill of the older specs' plans** — unrelated.
