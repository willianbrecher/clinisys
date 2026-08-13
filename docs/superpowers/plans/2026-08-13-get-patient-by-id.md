# Fix GET /api/patients/{id} Implementation Plan

> Implement task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop `GET /api/patients/{id}` from failing unconditionally (#30) by giving patients a
dedicated by-id query instead of reusing the paginated list query.
Spec: `docs/superpowers/specs/2026-08-13-get-patient-by-id.md`.

**Tech Stack:** .NET 8/C# 12, MediatR (backend only — no frontend changes needed).

## Global Constraints

- Follow root `CLAUDE.md`: branch `fix/<slug>` referencing issue #30.
- Single-layer (backend-only) change → PR uses `Closes #30`.
- Repo has a `bug` label matching the issue's own label — use it.

---

### Task 1: Add a dedicated GetPatientById query (#30)

**Branch:** `fix/get-patient-by-id` → PR `Closes #30`

**Files:**
- Add: `backend/src/CliniSys.Application/Queries/Patients/GetPatientById/GetPatientByIdQuery.cs`
- Add: `backend/src/CliniSys.Application/Queries/Patients/GetPatientById/GetPatientByIdQueryHandler.cs`
- Modify: `backend/src/CliniSys.Api/Controllers/PatientsController.cs`

**Interfaces:** new `GetPatientByIdQuery : IQuery<PatientModel?>` (public, dispatched by the
controller only). No repository interface changes — reuses `IPatientRepository.GetByIdAsync`,
inherited from the generic `IRepository<T>` base.

- [ ] **Step 1: Add `GetPatientByIdQuery`**

Create `backend/src/CliniSys.Application/Queries/Patients/GetPatientById/GetPatientByIdQuery.cs`:

```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Queries.Patients.GetPatients;

namespace CliniSys.Application.Queries.Patients.GetPatientById;

/// <summary>Query to fetch a single patient by ID.</summary>
/// <param name="Id">Patient identifier.</param>
public record GetPatientByIdQuery(Guid Id) : IQuery<PatientModel?>;
```

- [ ] **Step 2: Add `GetPatientByIdQueryHandler`**

Create `backend/src/CliniSys.Application/Queries/Patients/GetPatientById/GetPatientByIdQueryHandler.cs`:

```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Queries.Patients.GetPatients;

namespace CliniSys.Application.Queries.Patients.GetPatientById;

/// <summary>Handler for <see cref="GetPatientByIdQuery"/>.</summary>
public class GetPatientByIdQueryHandler : IQueryHandler<GetPatientByIdQuery, PatientModel?>
{
    private readonly IPatientRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Patient repository.</param>
    public GetPatientByIdQueryHandler(IPatientRepository repo) => _repo = repo;

    /// <summary>Returns the patient with the given ID, or <see langword="null"/> if none exists.</summary>
    /// <param name="request">Query with the patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The patient, or <see langword="null"/>.</returns>
    public async Task<PatientModel?> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await _repo.GetByIdAsync(request.Id, cancellationToken);
        return patient is null
            ? null
            : new PatientModel(patient.Id, patient.FullName, patient.DateOfBirth,
                patient.Phone, patient.Email, patient.Notes, patient.IsActive);
    }
}
```

- [ ] **Step 3: Update `PatientsController.GetById` to dispatch the new query**

In `backend/src/CliniSys.Api/Controllers/PatientsController.cs`:

1. Add to the `using` list (alongside the existing `CliniSys.Application.Queries.Patients.GetPatients`):

```csharp
using CliniSys.Application.Queries.Patients.GetPatientById;
```

2. Replace (`:37-43`):

```csharp
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
{
    var result = await _mediator.Send(new GetPatientsQuery(null, 1, 1000), ct);
    var patient = result.Items.FirstOrDefault(p => p.Id == id);
    return patient is null ? NotFound() : Ok(patient);
}
```

with:

```csharp
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
{
    var patient = await _mediator.Send(new GetPatientByIdQuery(id), ct);
    return patient is null ? NotFound() : Ok(patient);
}
```

- [ ] **Step 4: Build and manually verify**

- `dotnet build` on the backend solution/`CliniSys.Application` + `CliniSys.Api` projects —
  confirms the new query/handler wire up correctly via MediatR's assembly scanning (same
  registration mechanism already picking up `GetDoctorByIdQuery`, no DI changes needed).
- Via the `run` skill or a direct API call: `GET /api/patients/{id}` for an existing patient
  returns `200` with the patient payload (previously always `400`).
- `GET /api/patients/{id}` for a non-existent GUID returns `404` (unchanged `NotFound()` path).
- Through the UI: opening "Edit" on any patient now loads the form populated with that patient's
  data (previously showed "Failed to load patient." immediately).
- `GET /api/patients` (the list endpoint, `GetPatientsQuery`) still works unaffected — confirms
  the shared `PatientModel` record wasn't broken by being referenced from the new namespace.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CliniSys.Application/Queries/Patients/GetPatientById backend/src/CliniSys.Api/Controllers/PatientsController.cs
git commit -m "fix: add dedicated GetPatientById query instead of reusing the paginated list query"
```

- [ ] **Step 6: Open PR**

```bash
gh pr create --title "fix: add dedicated GetPatientById query instead of reusing the paginated list query" \
  --body "Closes #30

\`PatientsController.GetById\` called \`GetPatientsQuery(null, 1, 1000)\` and filtered client-side — but \`GetPatientsQueryHandler\` rejects any \`pageSize > 100\`, so the endpoint failed unconditionally, every time. Adds a dedicated \`GetPatientByIdQuery\`/\`GetPatientByIdQueryHandler\`, mirroring the existing \`GetDoctorById\` fix (simpler here — \`IPatientRepository.GetByIdAsync\`, inherited from the generic repo base, is enough; no eager-load/custom repository method needed since \`Patient\` has no related entity like \`Doctor\`/\`User\`).

Spec: \`docs/superpowers/specs/2026-08-13-get-patient-by-id.md\`" \
  --label bug --assignee willianbrecher
```
