# CliniSys — Fix GET /api/patients/{id} Spec

Date: 2026-08-13
Status: Draft
Issue: [#30](https://github.com/willianbrecher/clinisys/issues/30)

## 1. Goal

`GET /api/patients/{id}` should return the patient, not fail unconditionally. This is the first
call the edit-patient form makes, so today editing any patient is completely broken.

## 2. Current behavior — confirmed

`PatientsController.GetById` (`backend/src/CliniSys.Api/Controllers/PatientsController.cs:37-43`)
fetches a single patient by reusing the paginated list query with a huge page size and filtering
client-side:

```csharp
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
{
    var result = await _mediator.Send(new GetPatientsQuery(null, 1, 1000), ct);
    var patient = result.Items.FirstOrDefault(p => p.Id == id);
    return patient is null ? NotFound() : Ok(patient);
}
```

`GetPatientsQueryHandler.Handle` (`backend/src/CliniSys.Application/Queries/Patients/GetPatients/GetPatientsQueryHandler.cs:34`)
rejects any `PageSize > 100`:

```csharp
if (request.PageSize > 100) throw new ValidationException("PageSize cannot exceed 100.");
```

Since the controller always requests `pageSize: 1000`, this fails unconditionally, regardless of
whether the target patient exists — `GET /api/patients/{id}` never succeeds.

This is the identical bug already fixed for doctors. `DoctorsController.GetById`
(`backend/src/CliniSys.Api/Controllers/DoctorsController.cs:34-38`) now does:

```csharp
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
{
    var doctor = await _mediator.Send(new GetDoctorByIdQuery(id), ct);
    return doctor is null ? NotFound() : Ok(doctor);
}
```

backed by a dedicated `GetDoctorByIdQuery`/`GetDoctorByIdQueryHandler`
(`backend/src/CliniSys.Application/Queries/Doctors/GetDoctorById/`), which calls
`IDoctorRepository.GetByIdWithUserAsync` — a *custom* repository method, because `Doctor` needs its
related `User` navigation property eager-loaded for the DTO mapping (name/email live on `User`).

## 3. Proposed fix

Same dedicated-query pattern, but **simpler than doctors** — `Patient` has no related entity to
eager-load. `IPatientRepository` (`backend/src/CliniSys.Application/Common/Interfaces/Repositories/IPatientRepository.cs`)
already inherits `GetByIdAsync(Guid, CancellationToken)` from the generic `IRepository<T>` base
(`backend/src/CliniSys.Application/Common/Interfaces/Repositories/IRepository.cs:11`) — the same
method `UpdatePatientCommandHandler` already uses. **No new repository method needed.**

New files, mirroring `GetDoctorById`'s structure exactly:

`backend/src/CliniSys.Application/Queries/Patients/GetPatientById/GetPatientByIdQuery.cs`:

```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Queries.Patients.GetPatients;

namespace CliniSys.Application.Queries.Patients.GetPatientById;

/// <summary>Query to fetch a single patient by ID.</summary>
/// <param name="Id">Patient identifier.</param>
public record GetPatientByIdQuery(Guid Id) : IQuery<PatientModel?>;
```

`backend/src/CliniSys.Application/Queries/Patients/GetPatientById/GetPatientByIdQueryHandler.cs`:

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

(`PatientModel` is the existing record already defined in `GetPatientsQueryHandler.cs:16-17` —
reused via the `GetPatients` namespace import, same as the query file does. No duplicate model.)

`PatientsController.GetById` (`:37-43`) becomes:

```csharp
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
{
    var patient = await _mediator.Send(new GetPatientByIdQuery(id), ct);
    return patient is null ? NotFound() : Ok(patient);
}
```

with the controller's `using` list gaining
`CliniSys.Application.Queries.Patients.GetPatientById` (mirroring
`CliniSys.Application.Queries.Doctors.GetDoctorById` in `DoctorsController.cs:3`).

## 4. Non-goals

- No change to `GetPatientsQuery`/`GetPatientsQueryHandler` (the list endpoint) — it already works
  correctly for `pageSize <= 100`; this fix only stops the single-record endpoint from reusing it.
- No new `IPatientRepository` method — `GetByIdAsync` from the generic `IRepository<Patient>` base
  is sufficient, since `Patient` has no related entity needing eager-load (unlike `Doctor`/`User`).
- No frontend change — `getPatientById`/`PatientFormContent.tsx` already call the correct URL and
  shape; they were only ever broken by the backend 400, not by anything on their side.
