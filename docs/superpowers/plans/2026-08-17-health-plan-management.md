# Health Plan Management Implementation Plan

> Implement task-by-task, in order. Steps use checkbox (`- [ ]`) syntax for tracking. **Merge
> Task 1's PR before starting Task 2** — the frontend has nothing to call until the backend's
> `HealthPlan` CRUD and `Patient` linkage are live.

**Goal:** Add a managed `HealthPlan` catalog (Name, Notes) with its own list/create/edit UI, and
link `Patient` to it via a selectable reference (`HealthPlanId`) plus a per-patient free-text
membership number (`HealthPlanNumber`) — per the redesigned #5.
Spec: `docs/superpowers/specs/2026-08-17-health-plan-management.md`.

**Tech Stack:** .NET 8/C# 12 + EF Core migration (task 1), React 18/TypeScript/React Hook
Form/Yup (task 2).

## Global Constraints

- Follow root `CLAUDE.md`: one branch (`feature/<slug>`) per PR, referencing #5.
- This spans backend + frontend → **two PRs, both `Refs #5`** (never `Closes #5` on either —
  close the issue manually once both merge).
- Issue is `enhancement`-labeled — use `enhancement` on both PRs.
- Implementation order: **Task 1 (backend) → Task 2 (frontend)**.

---

### Task 1: `HealthPlan` entity, management CRUD, and `Patient` linkage (#5, backend)

**Branch:** `feature/health-plan-management-backend` → PR `Refs #5`

**Files:**
- Add: `backend/src/CliniSys.Domain/Entities/HealthPlan.cs`
- Modify: `backend/src/CliniSys.Domain/Entities/Patient.cs`
- Modify: `backend/src/CliniSys.Infrastructure/Persistence/AppDbContext.cs`
- Add: `backend/src/CliniSys.Application/Common/Interfaces/Repositories/IHealthPlanRepository.cs`
- Add: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/HealthPlanRepository.cs`
- Modify: `backend/src/CliniSys.Application/Common/Interfaces/Repositories/IPatientRepository.cs`
- Modify: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/PatientRepository.cs`
- Modify: `backend/src/CliniSys.Infrastructure/DependencyInjection.cs`
- Add: `backend/src/CliniSys.Application/Commands/HealthPlans/CreateHealthPlan/{CreateHealthPlanCommand,CreateHealthPlanCommandHandler,CreateHealthPlanCommandValidator}.cs`
- Add: `backend/src/CliniSys.Application/Commands/HealthPlans/UpdateHealthPlan/{UpdateHealthPlanCommand,UpdateHealthPlanCommandHandler,UpdateHealthPlanCommandValidator}.cs`
- Add: `backend/src/CliniSys.Application/Commands/HealthPlans/DeactivateHealthPlan/{DeactivateHealthPlanCommand,DeactivateHealthPlanCommandHandler}.cs`
- Add: `backend/src/CliniSys.Application/Queries/HealthPlans/GetHealthPlans/{GetHealthPlansQuery,GetHealthPlansQueryHandler}.cs`
- Add: `backend/src/CliniSys.Application/Queries/HealthPlans/GetHealthPlanById/{GetHealthPlanByIdQuery,GetHealthPlanByIdQueryHandler}.cs`
- Add: `backend/src/CliniSys.Api/Requests/HealthPlans/{CreateHealthPlanRequest,UpdateHealthPlanRequest}.cs`
- Add: `backend/src/CliniSys.Api/Controllers/HealthPlansController.cs`
- Modify: `backend/src/CliniSys.Application/Commands/Patients/CreatePatient/{CreatePatientCommand,CreatePatientCommandHandler,CreatePatientCommandValidator}.cs`
- Modify: `backend/src/CliniSys.Application/Commands/Patients/UpdatePatient/{UpdatePatientCommand,UpdatePatientCommandHandler,UpdatePatientCommandValidator}.cs`
- Modify: `backend/src/CliniSys.Application/Queries/Patients/GetPatients/GetPatientsQueryHandler.cs`
- Modify: `backend/src/CliniSys.Application/Queries/Patients/GetPatientById/GetPatientByIdQueryHandler.cs`
- Modify: `backend/src/CliniSys.Api/Requests/Patients/{CreatePatientRequest,UpdatePatientRequest}.cs`
- Modify: `backend/src/CliniSys.Api/Controllers/PatientsController.cs`
- Add: EF Core migration (generated)

**Interfaces:** new `IHealthPlanRepository`, new `HealthPlanModel` (public read DTO), `PatientModel`
gains three fields, `IPatientRepository` gains `GetByIdWithHealthPlanAsync`.

- [ ] **Step 1: Add the `HealthPlan` entity**

Create `backend/src/CliniSys.Domain/Entities/HealthPlan.cs`:

```csharp
namespace CliniSys.Domain.Entities;

/// <summary>A registered health/insurance plan patients can be linked to.</summary>
public class HealthPlan
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }
    /// <summary>Plan name (selected by patients — kept consistent across the app).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Free-text details/notes about the plan.</summary>
    public string? Notes { get; set; }
    /// <summary>False when soft-deleted.</summary>
    public bool IsActive { get; set; } = true;
}
```

- [ ] **Step 2: Link `Patient` to `HealthPlan`**

In `Patient.cs`, after `Notes`:

```csharp
/// <summary>Optional notes (insurance, medical, etc.).</summary>
public string? Notes { get; set; }
/// <summary>Optional linked health plan.</summary>
public Guid? HealthPlanId { get; set; }
/// <summary>Navigation to the linked health plan.</summary>
public HealthPlan? HealthPlan { get; set; }
/// <summary>Optional patient's own membership/card number under the linked plan.</summary>
public string? HealthPlanNumber { get; set; }
```

- [ ] **Step 3: Configure both entities in `AppDbContext`**

Add the `DbSet`:

```csharp
public DbSet<HealthPlan> HealthPlans => Set<HealthPlan>();
```

Add the `HealthPlan` config, and change `Patient`'s from a single-line lambda to a block adding
the FK:

```csharp
builder.Entity<HealthPlan>(e => e.HasKey(hp => hp.Id));

builder.Entity<Patient>(e =>
{
    e.HasKey(p => p.Id);
    e.HasOne(p => p.HealthPlan).WithMany()
     .HasForeignKey(p => p.HealthPlanId).OnDelete(DeleteBehavior.Restrict);
});
```

(Replaces the existing `builder.Entity<Patient>(e => e.HasKey(p => p.Id));` line.)

- [ ] **Step 4: Add `IHealthPlanRepository`/`HealthPlanRepository`**

Create `backend/src/CliniSys.Application/Common/Interfaces/Repositories/IHealthPlanRepository.cs`:

```csharp
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;

namespace CliniSys.Application.Common.Interfaces.Repositories;

/// <summary>Repository for <see cref="HealthPlan"/> with name-search support.</summary>
public interface IHealthPlanRepository : IRepository<HealthPlan>
{
    /// <summary>Returns paginated active health plans, optionally filtered by name substring.</summary>
    /// <param name="search">Optional case-insensitive name filter.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PagedResult<HealthPlan>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct = default);
}
```

Create `backend/src/CliniSys.Infrastructure/Persistence/Repositories/HealthPlanRepository.cs`
(mirrors `PatientRepository.cs` exactly):

```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class HealthPlanRepository : Repository<HealthPlan>, IHealthPlanRepository
{
    public HealthPlanRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<HealthPlan>> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _set.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<HealthPlan>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
```

Register it in `DependencyInjection.cs`, alongside the other repositories:

```csharp
services.AddScoped<IHealthPlanRepository, HealthPlanRepository>();
```

- [ ] **Step 5: Add `IPatientRepository.GetByIdWithHealthPlanAsync` and eager-load in `GetPagedAsync`**

In `IPatientRepository.cs`, add:

```csharp
/// <summary>Finds a patient by ID, including the HealthPlan navigation. Returns <see langword="null"/> if none.</summary>
/// <param name="id">Patient identifier.</param>
/// <param name="ct">Cancellation token.</param>
Task<Patient?> GetByIdWithHealthPlanAsync(Guid id, CancellationToken ct = default);
```

In `PatientRepository.cs`, add the implementation and eager-load in `GetPagedAsync`:

```csharp
public Task<Patient?> GetByIdWithHealthPlanAsync(Guid id, CancellationToken ct = default) =>
    _set.Include(p => p.HealthPlan).FirstOrDefaultAsync(p => p.Id == id, ct);
```

```csharp
var query = _set.Include(p => p.HealthPlan).Where(p => p.IsActive);
```

(replaces `var query = _set.Where(p => p.IsActive);` in `GetPagedAsync`.)

- [ ] **Step 6: Add `HealthPlan` CQRS — create/update/deactivate/list/by-id**

`Commands/HealthPlans/CreateHealthPlan/CreateHealthPlanCommand.cs`:

```csharp
using CliniSys.Application.Common.Interfaces;

namespace CliniSys.Application.Commands.HealthPlans.CreateHealthPlan;

/// <summary>Command to register a new health plan.</summary>
/// <param name="Name">Plan name.</param>
/// <param name="Notes">Optional notes.</param>
public record CreateHealthPlanCommand(string Name, string? Notes) : ICommand<Guid>;
```

`CreateHealthPlanCommandHandler.cs`:

```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;

namespace CliniSys.Application.Commands.HealthPlans.CreateHealthPlan;

/// <summary>Handler for <see cref="CreateHealthPlanCommand"/>.</summary>
public class CreateHealthPlanCommandHandler : ICommandHandler<CreateHealthPlanCommand, Guid>
{
    private readonly IHealthPlanRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Health plan repository.</param>
    public CreateHealthPlanCommandHandler(IHealthPlanRepository repo) => _repo = repo;

    /// <summary>Creates a new health plan record and returns its ID.</summary>
    /// <param name="request">Health plan creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new health plan's <see cref="Guid"/>.</returns>
    public async Task<Guid> Handle(CreateHealthPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = new HealthPlan { Id = Guid.NewGuid(), Name = request.Name, Notes = request.Notes };
        await _repo.AddAsync(plan, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);
        return plan.Id;
    }
}
```

`CreateHealthPlanCommandValidator.cs`:

```csharp
using FluentValidation;

namespace CliniSys.Application.Commands.HealthPlans.CreateHealthPlan;

/// <summary>Validates <see cref="CreateHealthPlanCommand"/>.</summary>
public class CreateHealthPlanCommandValidator : AbstractValidator<CreateHealthPlanCommand>
{
    /// <summary>Defines validation rules.</summary>
    public CreateHealthPlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
```

`Commands/HealthPlans/UpdateHealthPlan/` — same three files, mirroring
`UpdatePatientCommand`/`Handler`/`Validator`'s shape: `UpdateHealthPlanCommand(Guid Id, string
Name, string? Notes) : ICommand<Unit>`; handler loads via `GetByIdAsync`, throws
`NotFoundException` if missing, assigns `Name`/`Notes`, calls `Update`+`SaveChangesAsync`;
validator has the same `Name` rule as create.

`Commands/HealthPlans/DeactivateHealthPlan/` — mirrors `DeactivatePatientCommand`/`Handler`
exactly: `DeactivateHealthPlanCommand(Guid Id) : ICommand<Unit>`; handler loads, throws
`NotFoundException` if missing, sets `IsActive = false`, calls `Update`+`SaveChangesAsync`.

`Queries/HealthPlans/GetHealthPlans/GetHealthPlansQuery.cs` (+ handler containing
`HealthPlanModel`) — mirrors `GetPatientsQuery`/`GetPatientsQueryHandler` exactly:

```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Models;

namespace CliniSys.Application.Queries.HealthPlans.GetHealthPlans;

/// <summary>Query to retrieve a paginated, searchable list of active health plans.</summary>
/// <param name="Search">Optional case-insensitive name filter.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 100).</param>
public record GetHealthPlansQuery(string? Search = null, int Page = 1, int PageSize = 20)
    : IPagedQuery<HealthPlanModel>;
```

```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using FluentValidation;

namespace CliniSys.Application.Queries.HealthPlans.GetHealthPlans;

/// <summary>Health plan response model.</summary>
/// <param name="Id">Health plan identifier.</param>
/// <param name="Name">Plan name.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="IsActive">Active status.</param>
public record HealthPlanModel(Guid Id, string Name, string? Notes, bool IsActive);

/// <summary>Handler for <see cref="GetHealthPlansQuery"/>.</summary>
public class GetHealthPlansQueryHandler : IQueryHandler<GetHealthPlansQuery, PagedResult<HealthPlanModel>>
{
    private readonly IHealthPlanRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Health plan repository.</param>
    public GetHealthPlansQueryHandler(IHealthPlanRepository repo) => _repo = repo;

    /// <summary>Returns a paginated filtered list of health plans.</summary>
    /// <param name="request">Query with filters and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated health plan list.</returns>
    public async Task<PagedResult<HealthPlanModel>> Handle(
        GetHealthPlansQuery request, CancellationToken cancellationToken)
    {
        if (request.PageSize > 100) throw new ValidationException("PageSize cannot exceed 100.");

        var paged = await _repo.GetPagedAsync(request.Search, request.Page, request.PageSize, cancellationToken);
        var items = paged.Items.Select(p => new HealthPlanModel(p.Id, p.Name, p.Notes, p.IsActive)).ToList();
        return new PagedResult<HealthPlanModel>(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }
}
```

`Queries/HealthPlans/GetHealthPlanById/` — mirrors `GetPatientByIdQuery`/`Handler` exactly (dedicated
by-id query from the start, reusing `HealthPlanModel` from the `GetHealthPlans` namespace, same as
`GetPatientByIdQuery` reuses `PatientModel` from `GetPatients`):

```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Queries.HealthPlans.GetHealthPlans;

namespace CliniSys.Application.Queries.HealthPlans.GetHealthPlanById;

/// <summary>Query to fetch a single health plan by ID.</summary>
/// <param name="Id">Health plan identifier.</param>
public record GetHealthPlanByIdQuery(Guid Id) : IQuery<HealthPlanModel?>;
```

```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Queries.HealthPlans.GetHealthPlans;

namespace CliniSys.Application.Queries.HealthPlans.GetHealthPlanById;

/// <summary>Handler for <see cref="GetHealthPlanByIdQuery"/>.</summary>
public class GetHealthPlanByIdQueryHandler : IQueryHandler<GetHealthPlanByIdQuery, HealthPlanModel?>
{
    private readonly IHealthPlanRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Health plan repository.</param>
    public GetHealthPlanByIdQueryHandler(IHealthPlanRepository repo) => _repo = repo;

    /// <summary>Returns the health plan with the given ID, or <see langword="null"/> if none exists.</summary>
    /// <param name="request">Query with the health plan ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health plan, or <see langword="null"/>.</returns>
    public async Task<HealthPlanModel?> Handle(GetHealthPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _repo.GetByIdAsync(request.Id, cancellationToken);
        return plan is null ? null : new HealthPlanModel(plan.Id, plan.Name, plan.Notes, plan.IsActive);
    }
}
```

- [ ] **Step 7: Add `HealthPlansController`**

`Requests/HealthPlans/CreateHealthPlanRequest.cs`: `public record CreateHealthPlanRequest(string
Name, string? Notes);`. `UpdateHealthPlanRequest.cs`: same shape.

`Controllers/HealthPlansController.cs` (mirrors `PatientsController.cs` exactly, minus the
`Deactivate` route staying even though no frontend button calls it yet — kept for API
completeness/symmetry with `Patients`/`Doctors`):

```csharp
using CliniSys.Api.Requests.HealthPlans;
using CliniSys.Application.Commands.HealthPlans.CreateHealthPlan;
using CliniSys.Application.Commands.HealthPlans.DeactivateHealthPlan;
using CliniSys.Application.Commands.HealthPlans.UpdateHealthPlan;
using CliniSys.Application.Queries.HealthPlans.GetHealthPlanById;
using CliniSys.Application.Queries.HealthPlans.GetHealthPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for managing health plan records.</summary>
[ApiController, Route("api/health-plans"), Authorize(Roles = "Admin,Staff")]
public class HealthPlansController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public HealthPlansController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns a paginated list of active health plans, optionally filtered by name.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetHealthPlansQuery(search, page, pageSize), ct));

    /// <summary>Returns a single health plan by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var plan = await _mediator.Send(new GetHealthPlanByIdQuery(id), ct);
        return plan is null ? NotFound() : Ok(plan);
    }

    /// <summary>Creates a new health plan.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHealthPlanRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateHealthPlanCommand(request.Name, request.Notes), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Updates a health plan's details.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdateHealthPlanRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateHealthPlanCommand(id, request.Name, request.Notes), ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a health plan (sets IsActive = false).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateHealthPlanCommand(id), ct);
        return NoContent();
    }
}
```

- [ ] **Step 8: Wire `HealthPlanId`/`HealthPlanNumber` through the patient create/update path**

`CreatePatientCommand.cs`/`UpdatePatientCommand.cs` — add trailing parameters:

```csharp
public record CreatePatientCommand(string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes,
    Guid? HealthPlanId, string? HealthPlanNumber) : ICommand<Guid>;
```

(same trailing addition on `UpdatePatientCommand`, which also has `Id` and returns `Unit`.)

`CreatePatientCommandHandler.cs` — add to the `Patient` initializer:
`HealthPlanId = request.HealthPlanId, HealthPlanNumber = request.HealthPlanNumber`.

`UpdatePatientCommandHandler.cs` — add assignments:
`patient.HealthPlanId = request.HealthPlanId; patient.HealthPlanNumber =
request.HealthPlanNumber;`.

`CreatePatientCommandValidator.cs`/`UpdatePatientCommandValidator.cs` — add:

```csharp
RuleFor(x => x.HealthPlanNumber).MaximumLength(50);
```

(No rule for `HealthPlanId` — optional FK reference, no existence check, per the spec's §2
precedent from `Appointment`'s `PatientId`/`DoctorId`.)

`CreatePatientRequest.cs`/`UpdatePatientRequest.cs` — add the same two trailing parameters.

`PatientsController.cs` — update `Create`/`Update`'s command construction to pass
`request.HealthPlanId, request.HealthPlanNumber` through.

- [ ] **Step 9: Extend `PatientModel` and resolve the plan name on read**

In `GetPatientsQueryHandler.cs`:

```csharp
public record PatientModel(Guid Id, string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes, bool IsActive,
    Guid? HealthPlanId, string? HealthPlanName, string? HealthPlanNumber);
```

Update its one construction call:

```csharp
new PatientModel(p.Id, p.FullName, p.DateOfBirth, p.Phone, p.Email, p.Notes, p.IsActive,
    p.HealthPlanId, p.HealthPlan?.Name, p.HealthPlanNumber)
```

In `GetPatientByIdQueryHandler.cs`, switch from `_repo.GetByIdAsync` to
`_repo.GetByIdWithHealthPlanAsync` and update its construction call the same way:

```csharp
var patient = await _repo.GetByIdWithHealthPlanAsync(request.Id, cancellationToken);
return patient is null
    ? null
    : new PatientModel(patient.Id, patient.FullName, patient.DateOfBirth,
        patient.Phone, patient.Email, patient.Notes, patient.IsActive,
        patient.HealthPlanId, patient.HealthPlan?.Name, patient.HealthPlanNumber);
```

- [ ] **Step 10: Register `HealthPlan` CQRS handlers require no manual DI step** (MediatR
  assembly-scans handlers automatically, same as every other command/query in this codebase — only
  the repository needed explicit registration, done in Step 4).

- [ ] **Step 11: Generate the EF Core migration**

From `backend/src/CliniSys.Infrastructure`:

```bash
dotnet ef migrations add AddHealthPlanManagement --startup-project ../CliniSys.Api
```

- [ ] **Step 12: Build and manually verify**

- `dotnet build` on the backend solution.
- Confirm the migration applies cleanly on next `dotnet run`.
- `POST /api/health-plans` with `{ "name": "Unimed", "notes": "..." }` → `201`; `GET
  /api/health-plans/{id}` returns it; `GET /api/health-plans` lists it.
- `POST /api/health-plans` with an empty `name` → `400` (matches `Patient.FullName`'s
  `NotEmpty` behavior).
- `PUT /api/health-plans/{id}` updates `name`/`notes` → `204`, reflected on a follow-up `GET`.
- `DELETE /api/health-plans/{id}` → `204`; the plan no longer appears in `GET
  /api/health-plans` (soft-deleted, `IsActive = false`), matching `Patient`'s deactivate
  behavior.
- `POST /api/patients` with a `healthPlanId` pointing at a real health plan and a
  `healthPlanNumber` → `201`; `GET /api/patients/{id}` returns `healthPlanId`,
  `healthPlanName` (resolved from the join), and `healthPlanNumber` correctly.
- `POST /api/patients` omitting both health-plan fields → still `201` (both optional).
- `GET /api/patients` (list) — items include the resolved `healthPlanName` for patients that
  have a plan set, confirming the `.Include` in `GetPagedAsync` works (no N+1/missing data).
- A `healthPlanNumber` over 50 chars → `400`.

- [ ] **Step 13: Commit**

```bash
git add backend/src/CliniSys.Domain/Entities/HealthPlan.cs backend/src/CliniSys.Domain/Entities/Patient.cs backend/src/CliniSys.Infrastructure/Persistence/AppDbContext.cs backend/src/CliniSys.Application/Common/Interfaces/Repositories/IHealthPlanRepository.cs backend/src/CliniSys.Application/Common/Interfaces/Repositories/IPatientRepository.cs backend/src/CliniSys.Infrastructure/Persistence/Repositories/HealthPlanRepository.cs backend/src/CliniSys.Infrastructure/Persistence/Repositories/PatientRepository.cs backend/src/CliniSys.Infrastructure/DependencyInjection.cs backend/src/CliniSys.Application/Commands/HealthPlans backend/src/CliniSys.Application/Queries/HealthPlans backend/src/CliniSys.Api/Requests/HealthPlans backend/src/CliniSys.Api/Controllers/HealthPlansController.cs backend/src/CliniSys.Application/Commands/Patients backend/src/CliniSys.Application/Queries/Patients backend/src/CliniSys.Api/Requests/Patients backend/src/CliniSys.Api/Controllers/PatientsController.cs backend/src/CliniSys.Infrastructure/Persistence/Migrations
git commit -m "feat: add health plan management and link patients to a plan (backend)"
```

- [ ] **Step 14: Open PR**

```bash
gh pr create --title "feat: add health plan management and link patients to a plan (backend)" \
  --body "Refs #5

Adds a managed \`HealthPlan\` catalog (Name, Notes) with full CRUD, mirroring the existing \`Patient\` management pattern one-for-one. Links \`Patient\` to it via \`HealthPlanId\` (FK, selected by the frontend from the registered list — no existence check in the validator, matching \`Appointment\`'s \`PatientId\`/\`DoctorId\` precedent) plus a per-patient \`HealthPlanNumber\` (free text, the patient's own membership number under the plan). \`PatientModel\` resolves the plan's name via an eager-loaded join so the frontend never needs a second lookup.

Spec: \`docs/superpowers/specs/2026-08-17-health-plan-management.md\`" \
  --label enhancement --assignee willianbrecher
```

---

### Task 2: Health Plans UI + patient form linkage (#5, frontend)

**Branch:** `feature/health-plan-management-frontend` → PR `Refs #5`. Branch from `master` after
Task 1's PR merges.

**Files:**
- Modify: `frontend/src/api/types.ts`
- Add: `frontend/src/api/healthPlans.ts`
- Add: `frontend/src/features/healthPlans/healthPlan.schema.ts`
- Add: `frontend/src/features/healthPlans/HealthPlanFormContent.tsx`
- Add: `frontend/src/features/healthPlans/HealthPlansPage.tsx`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/components/AppLayout.tsx`
- Modify: `frontend/src/features/patients/patient.schema.ts`
- Modify: `frontend/src/features/patients/PatientFormContent.tsx`
- Modify: `frontend/src/locales/en-US/translation.json`
- Modify: `frontend/src/locales/pt-BR/translation.json`
- Modify: `frontend/src/locales/es-ES/translation.json`

**Interfaces:** new `HealthPlanModel`/`HealthPlanFormData`, `PatientModel`/`PatientFormData` gain
three fields.

- [ ] **Step 1: Add locale keys to all three bundles**

`en-US/translation.json` — new `healthPlans` section (after `doctors`, before `appointments`,
matching nav order from Step 6):

```json
"healthPlans": {
  "title": "Health Plans",
  "new": "New Health Plan",
  "name": "Name",
  "notes": "Notes"
}
```

Add to the existing `nav` block (after `"doctors"`):

```json
"healthPlans": "Health Plans",
```

Add to the existing `patients` block (after `"notes"`):

```json
"healthPlan": "Health Plan",
"healthPlanNumber": "Health Plan Number"
```

`pt-BR/translation.json` — mirror with:

```json
"healthPlans": {
  "title": "Planos de Saúde",
  "new": "Novo Plano de Saúde",
  "name": "Nome",
  "notes": "Observações"
}
```
`nav.healthPlans`: `"Planos de Saúde"`. `patients.healthPlan`: `"Plano de Saúde"`.
`patients.healthPlanNumber`: `"Número do Plano de Saúde"`.

`es-ES/translation.json` — mirror with:

```json
"healthPlans": {
  "title": "Planes de Salud",
  "new": "Nuevo Plan de Salud",
  "name": "Nombre",
  "notes": "Notas"
}
```
`nav.healthPlans`: `"Planes de Salud"`. `patients.healthPlan`: `"Plan de Salud"`.
`patients.healthPlanNumber`: `"Número del Plan de Salud"`.

- [ ] **Step 2: Add `HealthPlanModel` and extend `PatientModel`**

In `api/types.ts`:

```ts
export interface HealthPlanModel {
  id: string;
  name: string;
  notes?: string;
  isActive: boolean;
}
```

```ts
export interface PatientModel {
  id: string;
  fullName: string;
  dateOfBirth: string;
  phone: string;
  email?: string;
  notes?: string;
  isActive: boolean;
  healthPlanId?: string;
  healthPlanName?: string;
  healthPlanNumber?: string;
}
```

- [ ] **Step 3: Add `api/healthPlans.ts`**

Mirrors `api/patients.ts`:

```ts
import client from "./client";
import type { PagedResult, HealthPlanModel } from "./types";

export const getHealthPlans = (params: { search?: string; page?: number; pageSize?: number }) =>
  client.get<PagedResult<HealthPlanModel>>("/api/health-plans", { params }).then((r) => r.data);

export const getHealthPlanById = (id: string) =>
  client.get<HealthPlanModel>(`/api/health-plans/${id}`).then((r) => r.data);

export const createHealthPlan = (data: { name: string; notes?: string }) =>
  client.post<{ id: string }>("/api/health-plans", data).then((r) => r.data.id);

export const updateHealthPlan = (id: string, data: { name: string; notes?: string }) =>
  client.put(`/api/health-plans/${id}`, data);
```

- [ ] **Step 4: Add `healthPlan.schema.ts`**

```ts
import * as yup from "yup";

export const healthPlanSchema = yup.object({
  name: yup.string().required("Name is required").max(200),
  notes: yup.string().optional(),
});

export type HealthPlanFormData = yup.InferType<typeof healthPlanSchema>;
```

- [ ] **Step 5: Add `HealthPlanFormContent.tsx`**

Mirrors `PatientFormContent.tsx`'s structure, minus the fields health plans don't have (`name` +
`notes` only):

```tsx
import { useEffect } from "react";
import { useParams, useOutletContext } from "react-router-dom";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { getHealthPlanById, createHealthPlan, updateHealthPlan } from "@/api/healthPlans";
import { healthPlanSchema, type HealthPlanFormData } from "./healthPlan.schema";
import type { ModalContext } from "@/types/modal";

export function HealthPlanFormContent() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { onClose, onSaved } = useOutletContext<ModalContext>();
  const isEdit = !!id;

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<HealthPlanFormData>({
    resolver: yupResolver(healthPlanSchema) as unknown as Resolver<HealthPlanFormData>,
  });

  useEffect(() => {
    if (id) {
      getHealthPlanById(id).then((p) => reset({
        name: p.name, notes: p.notes ?? "",
      })).catch(() => toast.error("Failed to load health plan."));
    }
  }, [id, reset]);

  const onSubmit = async (data: HealthPlanFormData) => {
    try {
      if (isEdit) {
        await updateHealthPlan(id!, data);
        toast.success("Health plan updated.");
      } else {
        await createHealthPlan(data);
        toast.success("Health plan created.");
      }
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to save health plan.");
    }
  };

  return (
    <>
      <DialogHeader>
        <DialogTitle>{isEdit ? t("common.edit") : t("healthPlans.new")}</DialogTitle>
      </DialogHeader>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label>{t("healthPlans.name")}</Label>
          <Input {...register("name")} />
          {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("healthPlans.notes")}</Label>
          <Textarea {...register("notes")} rows={3} />
        </div>

        <div className="flex gap-2">
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? t("common.loading") : t("common.save")}
          </Button>
          <Button type="button" variant="outline" onClick={onClose}>
            {t("common.cancel")}
          </Button>
        </div>
      </form>
    </>
  );
}
```

- [ ] **Step 6: Add `HealthPlansPage.tsx`**

Mirrors `PatientsPage.tsx`'s structure (search + pagination + list + new/edit dialog outlet), with
columns `Name`, actions only (no `Phone`/`Email` — health plans don't have those), and **no
deactivate/delete button** in the list (per the spec's precedent from removing that action from
Patients).

- [ ] **Step 7: Add routes in `App.tsx`**

Mirroring the `/patients` route block:

```tsx
import { HealthPlansPage } from "@/features/healthPlans/HealthPlansPage";
import { HealthPlanFormContent } from "@/features/healthPlans/HealthPlanFormContent";
```

```tsx
<Route path="health-plans"
  element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><HealthPlansPage /></ProtectedRoute>}>
  <Route path="new"      element={<HealthPlanFormContent />} />
  <Route path=":id/edit" element={<HealthPlanFormContent />} />
</Route>
```

- [ ] **Step 8: Add the nav entry in `AppLayout.tsx`**

Add `CreditCard` to the `lucide-react` import list. Insert into `topLinks`, after `doctors` and
before `appointments`:

```tsx
{ to: "/health-plans", icon: <CreditCard className="h-4 w-4" />, label: t("nav.healthPlans"), roles: ["Admin","Staff"] },
```

- [ ] **Step 9: Extend `patientSchema` and the patient form**

In `patient.schema.ts`, after `notes`:

```ts
healthPlanId: yup.string().uuid().optional(),
healthPlanNumber: yup.string().max(50).optional(),
```

In `PatientFormContent.tsx`:

1. Import `getHealthPlans` and `HealthPlanModel`, add local state, fetch on mount:

```tsx
import { getHealthPlans } from "@/api/healthPlans";
import type { HealthPlanModel } from "@/api/types";
```

```tsx
const [healthPlans, setHealthPlans] = useState<HealthPlanModel[]>([]);

useEffect(() => {
  getHealthPlans({ pageSize: 100 }).then((r) => setHealthPlans(r.items)).catch(() => {});
}, []);
```

(Add `useState` to the existing `import { useEffect } from "react";` line.)

2. Add both fields to the edit-load `reset()` call:

```tsx
getPatientById(id).then((p) => reset({
  fullName: p.fullName, dateOfBirth: p.dateOfBirth,
  phone: p.phone, email: p.email ?? "", notes: p.notes ?? "",
  healthPlanId: p.healthPlanId ?? "", healthPlanNumber: p.healthPlanNumber ?? "",
})).catch(() => toast.error("Failed to load patient."));
```

3. Add the two fields between `email` and `notes`:

```tsx
<div className="flex flex-col gap-1.5">
  <Label>{t("patients.healthPlan")}</Label>
  <select className="border rounded px-3 py-2 text-sm bg-background" {...register("healthPlanId")}>
    <option value="">{t("common.select")}</option>
    {healthPlans.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
  </select>
  {errors.healthPlanId && <p className="text-xs text-destructive">{errors.healthPlanId.message}</p>}
</div>

<div className="flex flex-col gap-1.5">
  <Label>{t("patients.healthPlanNumber")}</Label>
  <Input {...register("healthPlanNumber")} />
  {errors.healthPlanNumber && <p className="text-xs text-destructive">{errors.healthPlanNumber.message}</p>}
</div>
```

- [ ] **Step 10: Manually verify via the `run` skill**

- Health Plans page: create a plan (name + notes), see it in the list, edit it, confirm changes
  persist.
- Creating a plan with an empty name shows an inline validation error, doesn't submit.
- Patient form: the Health Plan dropdown lists the plans just created; selecting one and saving
  a patient, then reopening it for edit, shows the same plan selected and the number populated.
- Leaving the Health Plan dropdown on "Select..." and health plan number blank still saves the
  patient successfully (both optional).
- An existing patient created before this change (no health plan set) opens with the dropdown on
  "Select..." and the number field empty — no error.
- Patients list table is unchanged — no new columns.
- Switch language (en-US/pt-BR/es-ES) — "Health Plans" nav label, the management page's labels,
  and the two new patient-form labels all translate correctly.

- [ ] **Step 11: Commit**

```bash
git add frontend/src/api/types.ts frontend/src/api/healthPlans.ts frontend/src/features/healthPlans frontend/src/App.tsx frontend/src/components/AppLayout.tsx frontend/src/features/patients/patient.schema.ts frontend/src/features/patients/PatientFormContent.tsx frontend/src/locales/en-US/translation.json frontend/src/locales/pt-BR/translation.json frontend/src/locales/es-ES/translation.json
git commit -m "feat: add health plans management UI and link to patient form (frontend)"
```

- [ ] **Step 12: Open PR**

```bash
gh pr create --title "feat: add health plans management UI and link to patient form (frontend)" \
  --body "Refs #5

Adds a Health Plans management page (list/create/edit), structurally identical to the existing Patients page, plus a new nav entry. The patient form gains a Health Plan dropdown (populated from the registered list — selecting keeps plan names consistent across patients) and a free-text Health Plan Number field (the patient's own membership number, distinct from the plan's own record). No changes to the patients list table.

Spec: \`docs/superpowers/specs/2026-08-17-health-plan-management.md\`" \
  --label enhancement --assignee willianbrecher
```

**After this PR merges, close #5 manually** (both its PRs will have landed).
