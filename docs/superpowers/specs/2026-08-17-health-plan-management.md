# CliniSys — Health Plan Management Spec

Date: 2026-08-17
Status: Draft — **not yet committed, pending review**
Issue: [#5](https://github.com/willianbrecher/clinisys/issues/5)

## 1. Goal

Add a managed **Health Plan** catalog (Name, Notes) with its own list/create/edit UI —
mirroring the existing `Patient`/`Doctor` management pattern — and link `Patient` to it via a
selectable reference instead of free text, so the same plan is never spelled differently across
patients. The patient's own membership number under that plan stays as a free-text field on the
patient, since it's per-patient, not per-plan.

## 2. Current state — confirmed

`Patient` (`backend/src/CliniSys.Domain/Entities/Patient.cs`) has no health-plan-related fields at
all — the earlier attempt at this issue (plain free-text `HealthPlanName`/`HealthPlanNumber` on
`Patient`) was undone before implementation; `Patient` is back to its original shape: `Id,
FullName, DateOfBirth, Phone, Email, Notes, IsActive`.

The closest existing analog for a new standalone managed entity (no auth/user linkage, just a
plain CRUD list) is `Patient` itself — full create/edit/list/search/deactivate, no relationships
in. `Doctor` is the closest analog for a **referenced-by-FK-with-eager-load** pattern: `Doctor`
doesn't eager-load its `User` navigation via the generic `IRepository<T>.GetByIdAsync`, so
`IDoctorRepository` adds a dedicated `GetByIdWithUserAsync` that does
`_set.Include(d => d.User).FirstOrDefaultAsync(...)` — this is the pattern `Patient`→`HealthPlan`
needs too, for the same reason (`Repository<T>.GetByIdAsync` is a plain `FindAsync`, no
`Include`).

`AppDbContext.OnModelCreating` already has a working FK-with-Restrict precedent for cross-entity
references: `Appointment`'s `PatientId`/`DoctorId`:

```csharp
e.HasOne(a => a.Patient).WithMany()
 .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
```

`CreateAppointmentCommandValidator` does **not** verify `PatientId`/`DoctorId` actually exist —
it trusts the frontend only ever sends IDs from its own dropdown (populated from the real
patient/doctor list). This spec follows the same trust level for `Patient.HealthPlanId` — no
extra existence check in the command validator, consistent with the codebase's existing standard
for FK-reference fields.

## 3. Proposed design

### 3.1 Backend — new `HealthPlan` entity and management CRUD

`backend/src/CliniSys.Domain/Entities/HealthPlan.cs` (new):

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

`AppDbContext`:

```csharp
public DbSet<HealthPlan> HealthPlans => Set<HealthPlan>();
```

```csharp
builder.Entity<HealthPlan>(e => e.HasKey(hp => hp.Id));
```

Repository — mirrors `IPatientRepository`/`PatientRepository` exactly (paginated, name search via
`EF.Functions.ILike`):

- `IHealthPlanRepository : IRepository<HealthPlan>` with
  `GetPagedAsync(string? search, int page, int pageSize, ct)`.
- `HealthPlanRepository` implementation, registered in `DependencyInjection.cs`:
  `services.AddScoped<IHealthPlanRepository, HealthPlanRepository>();`

CQRS — mirrors the `Patients` folder structure one-for-one:

- `Commands/HealthPlans/CreateHealthPlan/{Command,Handler,Validator}` —
  `Name` (`NotEmpty().MaximumLength(200)`), `Notes`
  (optional, unvalidated — same as `Patient.Notes`).
- `Commands/HealthPlans/UpdateHealthPlan/{Command,Handler,Validator}` — same fields + `Id`.
- `Commands/HealthPlans/DeactivateHealthPlan/{Command,Handler}` — mirrors
  `DeactivatePatientCommand` (`IsActive = false`).
- `Queries/HealthPlans/GetHealthPlans/{Query,Handler}` — paginated + search, `HealthPlanModel(Id,
  Name, Notes, IsActive)`.
- `Queries/HealthPlans/GetHealthPlanById/{Query,Handler}` — dedicated by-id query from day one
  (not the paginated-list-reuse bug #30 had to fix after the fact).

`HealthPlansController` (`api/health-plans`):

```csharp
[ApiController, Route("api/health-plans"), Authorize(Roles = "Admin,Staff")]
```

`GET` (list), `GET/{id}`, `POST`, `PUT/{id}`, `DELETE/{id}` (deactivate) — same shape and role
gate as `PatientsController`.

> **Judgment call, flag for review**: roles set to `Admin,Staff` (same tier as Patients/Doctors
> management), not Admin-only like Users/Settings. Health plans feel more like day-to-day
> reference data (Staff already fully manage Patients) than security-sensitive admin config —
> but this is a guess, not stated in the issue. Easy to narrow to Admin-only if that's wrong.

### 3.2 Backend — link `Patient` to `HealthPlan`

`Patient.cs` gains:

```csharp
/// <summary>Optional linked health plan.</summary>
public Guid? HealthPlanId { get; set; }
/// <summary>Navigation to the linked health plan.</summary>
public HealthPlan? HealthPlan { get; set; }
/// <summary>Optional patient's own membership/card number under the linked plan.</summary>
public string? HealthPlanNumber { get; set; }
```

Note there is **no** `HealthPlanName` on `Patient` — the name only ever lives on `HealthPlan`
itself; `PatientModel` (the read-side DTO) resolves it via the FK join for display, so the
frontend never has to make a second lookup call just to show the plan's name in the patient list
or form.

`AppDbContext`'s `Patient` config block (currently a single-line lambda) becomes:

```csharp
builder.Entity<Patient>(e =>
{
    e.HasKey(p => p.Id);
    e.HasOne(p => p.HealthPlan).WithMany()
     .HasForeignKey(p => p.HealthPlanId).OnDelete(DeleteBehavior.Restrict);
});
```

`IPatientRepository` gains `GetByIdWithHealthPlanAsync` (mirrors `IDoctorRepository
.GetByIdWithUserAsync`):

```csharp
Task<Patient?> GetByIdWithHealthPlanAsync(Guid id, CancellationToken ct = default);
```

```csharp
public Task<Patient?> GetByIdWithHealthPlanAsync(Guid id, CancellationToken ct = default) =>
    _set.Include(p => p.HealthPlan).FirstOrDefaultAsync(p => p.Id == id, ct);
```

`PatientRepository.GetPagedAsync` gains `.Include(p => p.HealthPlan)` on its base query, so the
list endpoint can also resolve the plan name without N+1 lookups.

`CreatePatientCommand`/`UpdatePatientCommand` (+ handlers, validators, API request DTOs,
`PatientsController` mapping) gain two trailing parameters:
`Guid? HealthPlanId, string? HealthPlanNumber` (`HealthPlanNumber`:
`MaximumLength(50)`, optional — no existence check on `HealthPlanId`, per §2's precedent).

`PatientModel` (`GetPatientsQueryHandler.cs`, reused by `GetPatientByIdQueryHandler`) gains three
fields:

```csharp
public record PatientModel(Guid Id, string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes, bool IsActive,
    Guid? HealthPlanId, string? HealthPlanName, string? HealthPlanNumber);
```

`GetPatientByIdQueryHandler` switches from `_repo.GetByIdAsync` to
`_repo.GetByIdWithHealthPlanAsync` so `patient.HealthPlan?.Name` is populated; both construction
call sites map `p.HealthPlanId, p.HealthPlan?.Name, p.HealthPlanNumber`.

EF Core migration (generated, not hand-written):

```bash
dotnet ef migrations add AddHealthPlanManagement --startup-project ../CliniSys.Api
```

Creates the new `HealthPlans` table and two new nullable columns on `Patients`
(`HealthPlanId` as the FK, `HealthPlanNumber`).

### 3.3 Frontend — new Health Plans management feature

`frontend/src/features/healthPlans/` (new folder, mirrors `patients/`):

- `HealthPlansPage.tsx` — list + search + pagination + new/edit dialog outlet, structurally
  identical to `PatientsPage.tsx`. **No deactivate/delete action in the list UI** — follows the
  precedent set by "remove Deactivate action from patient list" (merged PR, `feature/remove-
  patient-deactivate`); the backend `DELETE` endpoint still exists (mirroring the pattern), it's
  just not wired to a button here either.
- `HealthPlanFormContent.tsx` — `name` (required), `notes` (optional),
  structurally identical to `PatientFormContent.tsx`.
- `healthPlan.schema.ts` — `name: yup.string().required().max(200)`,
  `notes: yup.string().optional()`.

`frontend/src/api/healthPlans.ts` (new) — mirrors `api/patients.ts`: `getHealthPlans`,
`getHealthPlanById`, `createHealthPlan`, `updateHealthPlan`, `deactivateHealthPlan`.

`api/types.ts`:

```ts
export interface HealthPlanModel {
  id: string; name: string; notes?: string; isActive: boolean;
}
```

`PatientModel` gains three fields (matching the backend's new `PatientModel` shape):

```ts
export interface PatientModel {
  id: string; fullName: string; dateOfBirth: string; phone: string;
  email?: string; notes?: string; isActive: boolean;
  healthPlanId?: string; healthPlanName?: string; healthPlanNumber?: string;
}
```

### 3.4 Frontend — link the patient form to Health Plans

`PatientFormContent.tsx` gains, between `email` and `notes` (same grouping decision as the earlier
undone attempt):

- A `healthPlanId` `<select>` populated via `getHealthPlans({ pageSize: 100 })` on mount (mirrors
  how `AppointmentFormContent` populates its `patients`/`doctors` dropdowns), showing
  `HealthPlan.Name`, with a blank/"None" option since the field is optional.
- A `healthPlanNumber` free-text `<Input>` (the patient's own membership number).

`patient.schema.ts` gains `healthPlanId: yup.string().uuid().optional()`,
`healthPlanNumber: yup.string().max(50).optional()`.

Patients list table (`PatientsPage.tsx`) — **unchanged**, same "detail-only" treatment `Notes`
already gets; no new columns.

### 3.5 Navigation

`AppLayout.tsx`'s `topLinks` (same tier as Patients/Doctors, not under the Administration group —
consistent with §3.1's role-gate judgment call):

```tsx
{ to: "/health-plans", icon: <CreditCard className="h-4 w-4" />, label: t("nav.healthPlans"), roles: ["Admin","Staff"] },
```

`App.tsx` gains routes: `/health-plans` (list) with nested `/new` and `/:id/edit`, mirroring the
`/patients` route block exactly.

### 3.6 Locales

New `healthPlans` section in all three bundles (`title`, `new`, `name`, `notes`), plus
`nav.healthPlans`, plus two new keys under the existing `patients` section for the patient-form
labels (`healthPlan`, `healthPlanNumber` — the select's label and the free-text number's label).

## 4. Non-goals

- No change to how `Notes` (patient's general notes) works — health plan fields are additive.
- No plan-tier/coverage-detail modeling (co-pay, coverage type, etc.) — `Notes` on `HealthPlan` is
  the free-text catch-all for anything not worth a dedicated field, per the issue's own framing.
- No cascading behavior when a `HealthPlan` is deactivated — `Restrict` on the FK, matching
  `Appointment`'s `Patient`/`Doctor` FKs; deactivating a plan doesn't touch patients referencing
  it (their `HealthPlanId` stays valid, they just wouldn't see it in the active-plans dropdown
  going forward — same as a deactivated `Patient`/`Doctor` staying referenced by past
  `Appointment`s).
- No `HealthPlanId` existence validation in the command validator — matches `Appointment`'s
  `PatientId`/`DoctorId` precedent (§2).
- No search/filter by health plan on the patients list — out of scope for this pass.
