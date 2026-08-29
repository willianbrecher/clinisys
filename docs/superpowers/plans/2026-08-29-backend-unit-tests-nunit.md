# Backend Unit Tests (NUnit) Implementation Plan

> Implement task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a NUnit test project covering `CliniSys.Application` (validators, handlers,
validation pipeline) and a GitHub Actions workflow running `dotnet test` on backend PRs (#60).
Spec: `docs/superpowers/specs/2026-08-29-backend-unit-tests-nunit.md`.

**Tech Stack:** .NET 8 / C# 12, NUnit 4, NSubstitute 5.x, FluentAssertions 7.x, GitHub Actions.

## Global Constraints

- Branches `feature/<slug>` referencing #60.
- Three PRs, all `Refs #60`; close #60 by hand once all merge. PRs 1+2 may be combined if the
  reviewer prefers one backend PR.
- Issue is labeled `enhancement`; no `feature` label in the repo — use `enhancement`.
- No comments that restate the test's intent — only a non-obvious WHY (the version-pin comments
  in the csproj are the main case).
- Every task ends with a green `dotnet test backend/CliniSys.sln` locally.

---

### Task 1: Test project scaffold + validator & pipeline tests (#60)

**Branch:** `feature/backend-test-project-nunit` → PR `Refs #60`

**Files:**
- Add: `backend/test/CliniSys.Application.Tests/CliniSys.Application.Tests.csproj`
- Add: `backend/test/CliniSys.Application.Tests/TestSupport/Builders.cs`
- Add: `backend/test/CliniSys.Application.Tests/Behaviours/ValidationBehaviourTests.cs`
- Add: `backend/test/CliniSys.Application.Tests/<Aggregate>/*ValidatorTests.cs` (one per validator, §3.4 table — grouped by aggregate folder, one level deep)
- Modify: `backend/CliniSys.sln`
- Modify: `backend/CLAUDE.md`

- [ ] **Step 1: Create the project**

Create `CliniSys.Application.Tests.csproj` per spec §3.2 (exact package list; add the WHY comment
above the `FluentAssertions` version range).

```bash
dotnet sln backend/CliniSys.sln add \
  backend/test/CliniSys.Application.Tests/CliniSys.Application.Tests.csproj \
  --solution-folder test
dotnet restore backend/CliniSys.sln
```

- [ ] **Step 2: `TestSupport/Builders.cs`**

Static factory helpers with valid defaults, each param overridable:
`Builders.ClinicSettings(openDays, openTime, closeTime)`, `Builders.Appointment(status, startsAt,
durationMinutes, doctorId)`, `Builders.CreateAppointmentCommand(...)`, `Builders.Patient(...)`,
`Builders.HealthPlan(...)`.

- [ ] **Step 3: `ValidationBehaviourTests`**

Throwaway `record FakeRequest(...) : IRequest<FakeResponse>` + `record FakeResponse(...)`. Cases
from spec §3.4 "Pipeline": no validators → `next` called once; passing validators → `next`
called; failing validator → `ValidationException` with the failures, `next` never called (assert
via a delegate spy / call counter).

- [ ] **Step 4: Validator fixtures**

One `*ValidatorTests.cs` per validator in spec §3.4's table, in the aggregate folder
(`Appointments/`, `Patients/`, …). Use `validator.TestValidate(cmd)` +
`ShouldHaveValidationErrorFor` / `ShouldNotHaveValidationErrorFor`. `[TestCase]` rows for numeric
bounds (`DurationMinutes` 4/5/60/480/481, etc.).

- [ ] **Step 5: `backend/CLAUDE.md`**

Replace the "There is no backend test project" paragraph with:
- `dotnet test` (run from `backend/`) command;
- test project location `test/CliniSys.Application.Tests`, one folder per aggregate (not
  mirroring the production `Commands/<Verb>/` nesting);
- the conventions from spec §3.5 (fixture-per-class, `Method_Scenario_Expected` naming, AAA,
  FluentAssertions, `Builders`).

- [ ] **Step 6: Verify**

`dotnet test backend/CliniSys.sln` — all green. `dotnet build backend/CliniSys.sln` still clean.

- [ ] **Step 7: Commit + PR**

```bash
git add backend/test backend/CliniSys.sln backend/CLAUDE.md
git commit -m "test: add NUnit test project with validator and pipeline tests"
gh pr create --base master \
  --title "test: add NUnit test project with validator and pipeline tests" \
  --body "Refs #60

First backend test project (\`test/CliniSys.Application.Tests\`, NUnit 4 + NSubstitute + FluentAssertions). Covers every FluentValidation validator and the \`ValidationBehaviour\` pipeline. Solution gains a \`test\` folder; \`backend/CLAUDE.md\` documents \`dotnet test\` and the test conventions.

FluentAssertions pinned to \`7.x\` (v8 went to a paid license); NSubstitute chosen over Moq (MIT, no build-time telemetry).

Spec: \`docs/superpowers/specs/2026-08-29-backend-unit-tests-nunit.md\`" \
  --label enhancement --assignee willianbrecher
```

---

### Task 2: Handler tests (#60)

**Branch:** `feature/backend-handler-tests` → PR `Refs #60`. Branch from master after Task 1 merges.

**Files:**
- Add: `backend/test/CliniSys.Application.Tests/Appointments/*HandlerTests.cs`
- Add: `backend/test/CliniSys.Application.Tests/Patients/*HandlerTests.cs`
- Add: `backend/test/CliniSys.Application.Tests/Doctors/*HandlerTests.cs`
- Add: `backend/test/CliniSys.Application.Tests/HealthPlans/*HandlerTests.cs`
- Add: `backend/test/CliniSys.Application.Tests/Account/*HandlerTests.cs`
- Extend: `TestSupport/Builders.cs` as needed

- [ ] **Step 1: Appointment handlers**
  - `CreateAppointmentCommandHandlerTests` — closed-day, before-open, after-close, overlap,
    cancelled-overlap-ignored, happy path (`AddAsync` + `SaveChangesAsync` verified).
  - `UpdateAppointmentStatusCommandHandlerTests` — not-found; `[TestCase]` over the full allowed
    transition matrix incl. no-op; a rejected transition → `ConflictException` + no save.
  - `RescheduleAppointmentCommandHandlerTests` — open-hours + overlap with `excludeId` = self.

- [ ] **Step 2: Query handlers**
  - `GetPatientByIdQueryHandlerTests` / `GetDoctorByIdQueryHandlerTests` — found → mapped model
    (patient incl. `HealthPlanName`); missing → `NotFoundException`; assert the dedicated repo
    method is used, never a paged list (regression guard for #30).
  - `GetPatientsQueryHandlerTests` / `GetDoctorsQueryHandlerTests` /
    `GetHealthPlansQueryHandlerTests` — `PageSize > 100` → `ValidationException`; search term +
    page params forwarded; items mapped.

- [ ] **Step 3: Health plan + account handlers**
  - `CreateHealthPlanCommandHandlerTests` / `UpdateHealthPlanCommandHandlerTests` /
    `DeactivateHealthPlanCommandHandlerTests` — create returns id + saves; update mutates + saves;
    missing id → `NotFoundException`; deactivate sets `IsActive = false`.
  - `UpdatePreferencesCommandHandlerTests` + `GetCurrentUserPreferencesQueryHandlerTests` —
    theme/language round-trip; null when the user is gone.

- [ ] **Step 4: Verify + commit + PR**

`dotnet test backend/CliniSys.sln` green.

```bash
git commit -m "test: add command/query handler tests for the Application layer"
gh pr create --base master \
  --title "test: add command/query handler tests for the Application layer" \
  --body "Refs #60

Handler-level coverage: appointment open-hours / overlap / status-transition guardrails, the dedicated by-id queries (regression guard for #30), the \`PageSize > 100\` cap, health-plan CRUD, and account preferences. All dependencies are substituted repository interfaces — no database.

Spec: \`docs/superpowers/specs/2026-08-29-backend-unit-tests-nunit.md\`" \
  --label enhancement --assignee willianbrecher
```

---

### Task 3: CI workflow + root docs (#60)

**Branch:** `feature/backend-tests-ci` → PR `Refs #60`. Merge after Task 1.

**Files:**
- Add: `.github/workflows/backend-tests.yml`
- Modify: `CLAUDE.md` (root)

- [ ] **Step 1: Workflow**

`.github/workflows/backend-tests.yml` exactly as spec §3.6 — `push` to master + `pull_request`
scoped to `backend/**` and the workflow file; restore / build Release / `dotnet test` with
`XPlat Code Coverage` collection, no gate.

- [ ] **Step 2: root `CLAUDE.md`**

Note under a suitable heading that backend changes now run `dotnet test` in CI (workflow
`backend-tests.yml`) and PRs must keep it green.

- [ ] **Step 3: Verify**

Push the branch, open the PR, confirm the `backend-tests` check runs and passes against Task 1's
merged tests. (If Task 2 hasn't merged yet, that's fine — the check covers whatever is on the
branch.)

- [ ] **Step 4: Commit + PR**

```bash
git add .github/workflows/backend-tests.yml CLAUDE.md
git commit -m "ci: run backend dotnet test on pull requests"
gh pr create --base master \
  --title "ci: run backend dotnet test on pull requests" \
  --body "Refs #60

The repo's first CI workflow. Runs \`dotnet restore\` / \`build -c Release\` / \`dotnet test\` on pushes to master and on PRs touching \`backend/**\`. Coverage is collected (\`XPlat Code Coverage\`) but not gated. Root \`CLAUDE.md\` notes the new check.

Spec: \`docs/superpowers/specs/2026-08-29-backend-unit-tests-nunit.md\`" \
  --label enhancement --assignee willianbrecher
```

- [ ] **Step 5:** Close #60 once Tasks 1–3 have all merged.
