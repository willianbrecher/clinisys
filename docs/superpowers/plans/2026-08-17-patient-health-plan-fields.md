# Add Health Plan Fields to Patient Implementation Plan

> Implement task-by-task, in order. Steps use checkbox (`- [ ]`) syntax for tracking. **Merge Task
> 1's PR before starting Task 2** — the frontend's `PatientModel`/schema additions are meaningless
> until the backend actually returns/accepts the new fields, and testing Task 2 end-to-end needs
> Task 1's API live.

**Goal:** Add optional `healthPlanName`/`healthPlanNumber` fields to the patient record (#5) —
plain fields on `Patient`, not a managed plan catalog.
Spec: `docs/superpowers/specs/2026-08-17-patient-health-plan-fields.md`.

**Tech Stack:** .NET 8/C# 12 + EF Core migration (task 1), React 18/TypeScript/React Hook
Form/Yup (task 2).

## Global Constraints

- Follow root `CLAUDE.md`: one branch (`feature/<slug>`) per PR, referencing #5.
- This spans backend + frontend → **two PRs, both `Refs #5`** (never `Closes #5` on either — close
  the issue manually once both merge).
- Issue is `enhancement`-labeled — use `enhancement` on both PRs.
- Implementation order: **Task 1 (backend) → Task 2 (frontend)**. Task 2 can't be meaningfully
  tested without Task 1's API changes live.

---

### Task 1: Add health plan fields to the backend (#5)

**Branch:** `feature/patient-health-plan-backend` → PR `Refs #5`

**Files:**
- Modify: `backend/src/CliniSys.Domain/Entities/Patient.cs`
- Modify: `backend/src/CliniSys.Application/Commands/Patients/CreatePatient/CreatePatientCommand.cs`
- Modify: `backend/src/CliniSys.Application/Commands/Patients/CreatePatient/CreatePatientCommandHandler.cs`
- Modify: `backend/src/CliniSys.Application/Commands/Patients/CreatePatient/CreatePatientCommandValidator.cs`
- Modify: `backend/src/CliniSys.Application/Commands/Patients/UpdatePatient/UpdatePatientCommand.cs`
- Modify: `backend/src/CliniSys.Application/Commands/Patients/UpdatePatient/UpdatePatientCommandHandler.cs`
- Modify: `backend/src/CliniSys.Application/Commands/Patients/UpdatePatient/UpdatePatientCommandValidator.cs`
- Modify: `backend/src/CliniSys.Application/Queries/Patients/GetPatients/GetPatientsQueryHandler.cs`
- Modify: `backend/src/CliniSys.Api/Requests/Patients/CreatePatientRequest.cs`
- Modify: `backend/src/CliniSys.Api/Requests/Patients/UpdatePatientRequest.cs`
- Modify: `backend/src/CliniSys.Api/Controllers/PatientsController.cs`
- Add: EF Core migration (generated, not hand-written)

**Interfaces:** `PatientModel` (in `GetPatientsQueryHandler.cs`, also consumed by
`GetPatientByIdQueryHandler.cs` unchanged — it only maps `Patient` → `PatientModel` positionally,
so the new fields flow through automatically once `PatientModel` and the mapping calls change)
gains two new trailing fields.

- [ ] **Step 1: Add the two properties to `Patient`**

In `Patient.cs`, after `Notes`:

```csharp
/// <summary>Optional notes (insurance, medical, etc.).</summary>
public string? Notes { get; set; }
/// <summary>Optional health plan name.</summary>
public string? HealthPlanName { get; set; }
/// <summary>Optional health plan number.</summary>
public string? HealthPlanNumber { get; set; }
```

- [ ] **Step 2: Update `CreatePatientCommand` + handler + validator**

`CreatePatientCommand.cs` — add trailing parameters:

```csharp
public record CreatePatientCommand(string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes,
    string? HealthPlanName, string? HealthPlanNumber) : ICommand<Guid>;
```

`CreatePatientCommandHandler.cs` — assign in the `Patient` initializer:

```csharp
var patient = new Patient
{
    Id = Guid.NewGuid(), FullName = request.FullName,
    DateOfBirth = request.DateOfBirth, Phone = request.Phone,
    Email = request.Email, Notes = request.Notes,
    HealthPlanName = request.HealthPlanName, HealthPlanNumber = request.HealthPlanNumber
};
```

`CreatePatientCommandValidator.cs` — add after the existing rules:

```csharp
RuleFor(x => x.HealthPlanName).MaximumLength(200);
RuleFor(x => x.HealthPlanNumber).MaximumLength(50);
```

(Both optional — no `NotEmpty()`, matching `Notes`. FluentValidation applies `MaximumLength` to
`null` values without error, same as how `Email`'s rule already behaves when absent.)

- [ ] **Step 3: Update `UpdatePatientCommand` + handler + validator**

Same three changes, mirrored in `UpdatePatientCommand.cs`, `UpdatePatientCommandHandler.cs`
(`patient.HealthPlanName = request.HealthPlanName; patient.HealthPlanNumber =
request.HealthPlanNumber;` alongside the other field assignments), and
`UpdatePatientCommandValidator.cs` (same two `MaximumLength` rules).

- [ ] **Step 4: Extend `PatientModel` and both its construction call sites**

In `GetPatientsQueryHandler.cs`:

```csharp
public record PatientModel(Guid Id, string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes, bool IsActive,
    string? HealthPlanName, string? HealthPlanNumber);
```

Update the one construction call in this file:

```csharp
new PatientModel(p.Id, p.FullName, p.DateOfBirth, p.Phone, p.Email, p.Notes, p.IsActive,
    p.HealthPlanName, p.HealthPlanNumber)
```

In `GetPatientByIdQueryHandler.cs`, update its construction call the same way:

```csharp
new PatientModel(patient.Id, patient.FullName, patient.DateOfBirth,
    patient.Phone, patient.Email, patient.Notes, patient.IsActive,
    patient.HealthPlanName, patient.HealthPlanNumber)
```

- [ ] **Step 5: Extend the API request DTOs and controller mapping**

`CreatePatientRequest.cs`/`UpdatePatientRequest.cs` — add the two trailing parameters, same shape
as their corresponding commands.

`PatientsController.cs` — update `Create` and `Update`'s command construction to pass the two new
fields through from `request`:

```csharp
var id = await _mediator.Send(new CreatePatientCommand(
    request.FullName, request.DateOfBirth, request.Phone, request.Email, request.Notes,
    request.HealthPlanName, request.HealthPlanNumber), ct);
```

```csharp
await _mediator.Send(new UpdatePatientCommand(
    id, request.FullName, request.DateOfBirth, request.Phone, request.Email, request.Notes,
    request.HealthPlanName, request.HealthPlanNumber), ct);
```

- [ ] **Step 6: Generate the EF Core migration**

From `backend/src/CliniSys.Infrastructure`:

```bash
dotnet ef migrations add AddPatientHealthPlanFields --startup-project ../CliniSys.Api
```

No manual `AppDbContext` configuration needed — `Patient`'s existing string columns (`Email`,
`Notes`) have no explicit `.Property()` config either; both new columns get the same default
nullable `text` mapping EF/Npgsql already uses for them.

- [ ] **Step 7: Build and manually verify**

- `dotnet build` on the backend solution.
- Confirm the migration applies cleanly on next `dotnet run` (auto-applies migrations on startup,
  per `backend/CLAUDE.md`).
- `POST /api/patients` with `healthPlanName`/`healthPlanNumber` set → `201`, and
  `GET /api/patients/{id}` for that patient returns both fields back correctly.
- `POST /api/patients` **omitting** both fields → still `201` (both optional, no `NotEmpty`).
- `PUT /api/patients/{id}` updating just `healthPlanNumber` → `204`, and a follow-up `GET` reflects
  the change, with other fields unaffected.
- `GET /api/patients` (list endpoint) — items include both new fields for patients that have them
  set, `null` for those that don't.
- A `healthPlanName` over 200 chars or `healthPlanNumber` over 50 chars → `400` validation error
  (mirrors the existing `FullName`/`Phone` length-cap behavior).

- [ ] **Step 8: Commit**

```bash
git add backend/src/CliniSys.Domain/Entities/Patient.cs backend/src/CliniSys.Application/Commands/Patients backend/src/CliniSys.Application/Queries/Patients/GetPatients/GetPatientsQueryHandler.cs backend/src/CliniSys.Api/Requests/Patients backend/src/CliniSys.Api/Controllers/PatientsController.cs backend/src/CliniSys.Infrastructure/Persistence/Migrations
git commit -m "feat: add health plan name/number fields to patient (backend)"
```

- [ ] **Step 9: Open PR**

```bash
gh pr create --title "feat: add health plan name/number fields to patient (backend)" \
  --body "Refs #5

Adds optional \`HealthPlanName\`/\`HealthPlanNumber\` fields to \`Patient\`, threaded through create/update commands (with a \`MaximumLength\` cap, no \`NotEmpty\` — matching \`Notes\`'s optional-field treatment), \`PatientModel\` (used by both the list and by-id queries), the API request DTOs, and a new EF Core migration. Plain fields, not a managed plan catalog, per the issue's own scope note.

Spec: \`docs/superpowers/specs/2026-08-17-patient-health-plan-fields.md\`" \
  --label enhancement --assignee willianbrecher
```

---

### Task 2: Add health plan fields to the frontend (#5)

**Branch:** `feature/patient-health-plan-frontend` → PR `Refs #5`. Branch from `master` after
Task 1's PR merges.

**Files:**
- Modify: `frontend/src/api/types.ts`
- Modify: `frontend/src/api/patients.ts`
- Modify: `frontend/src/features/patients/patient.schema.ts`
- Modify: `frontend/src/features/patients/PatientFormContent.tsx`
- Modify: `frontend/src/locales/en-US/translation.json`
- Modify: `frontend/src/locales/pt-BR/translation.json`
- Modify: `frontend/src/locales/es-ES/translation.json`

**Interfaces:** none new — extends the existing `PatientModel`/`PatientFormData` shapes.

- [ ] **Step 1: Add locale keys to all three bundles**

`en-US/translation.json`, inside `patients`, after `"notes"`:

```json
"healthPlanName": "Health Plan Name",
"healthPlanNumber": "Health Plan Number"
```

`pt-BR/translation.json`:

```json
"healthPlanName": "Nome do Plano de Saúde",
"healthPlanNumber": "Número do Plano de Saúde"
```

`es-ES/translation.json`:

```json
"healthPlanName": "Nombre del Plan de Salud",
"healthPlanNumber": "Número del Plan de Salud"
```

- [ ] **Step 2: Extend `PatientModel`**

In `api/types.ts`:

```ts
export interface PatientModel {
  id: string;
  fullName: string;
  dateOfBirth: string;
  phone: string;
  email?: string;
  notes?: string;
  isActive: boolean;
  healthPlanName?: string;
  healthPlanNumber?: string;
}
```

- [ ] **Step 3: Extend `createPatient`/`updatePatient` payload types**

In `api/patients.ts`, add to both inline types:

```ts
export const createPatient = (data: {
  fullName: string; dateOfBirth: string; phone: string; email?: string; notes?: string;
  healthPlanName?: string; healthPlanNumber?: string;
}) => client.post<{ id: string }>("/api/patients", data).then((r) => r.data.id);

export const updatePatient = (id: string, data: {
  fullName: string; dateOfBirth: string; phone: string; email?: string; notes?: string;
  healthPlanName?: string; healthPlanNumber?: string;
}) => client.put(`/api/patients/${id}`, data);
```

- [ ] **Step 4: Extend `patientSchema`**

In `patient.schema.ts`, after `notes`:

```ts
export const patientSchema = yup.object({
  fullName: yup.string().required("Full name is required").max(200),
  dateOfBirth: yup.string().required("Date of birth is required"),
  phone: yup.string().required("Phone is required").max(30),
  email: yup.string().email("Invalid email").optional(),
  notes: yup.string().optional(),
  healthPlanName: yup.string().max(200).optional(),
  healthPlanNumber: yup.string().max(50).optional(),
});
```

- [ ] **Step 5: Add the two fields to the form**

In `PatientFormContent.tsx`, add both new fields to the edit-load `reset()` call:

```tsx
getPatientById(id).then((p) => reset({
  fullName: p.fullName, dateOfBirth: p.dateOfBirth,
  phone: p.phone, email: p.email ?? "", notes: p.notes ?? "",
  healthPlanName: p.healthPlanName ?? "", healthPlanNumber: p.healthPlanNumber ?? "",
})).catch(() => toast.error("Failed to load patient."));
```

Add two new form fields between `email` and `notes` (grouping the two "administrative" fields
together):

```tsx
<div className="flex flex-col gap-1.5">
  <Label>{t("patients.healthPlanName")}</Label>
  <Input {...register("healthPlanName")} />
  {errors.healthPlanName && <p className="text-xs text-destructive">{errors.healthPlanName.message}</p>}
</div>

<div className="flex flex-col gap-1.5">
  <Label>{t("patients.healthPlanNumber")}</Label>
  <Input {...register("healthPlanNumber")} />
  {errors.healthPlanNumber && <p className="text-xs text-destructive">{errors.healthPlanNumber.message}</p>}
</div>
```

- [ ] **Step 6: Manually verify via the `run` skill**

- Create a new patient with health plan name/number filled in — saves successfully; reopening it
  for edit shows both values populated.
- Create a new patient leaving both blank — saves successfully (optional fields don't block
  submit).
- Edit an existing patient (created before this change, so both fields are `null` server-side) —
  form loads with both fields empty, not "undefined" or an error.
- Enter a health plan name over 200 chars — inline validation error, submit blocked.
- Switch language (en-US/pt-BR/es-ES) — both new field labels translate correctly.
- Patients list table is unchanged (no new columns) — confirms scope stayed to the form only.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/api/types.ts frontend/src/api/patients.ts frontend/src/features/patients/patient.schema.ts frontend/src/features/patients/PatientFormContent.tsx frontend/src/locales/en-US/translation.json frontend/src/locales/pt-BR/translation.json frontend/src/locales/es-ES/translation.json
git commit -m "feat: add health plan name/number fields to patient form (frontend)"
```

- [ ] **Step 8: Open PR**

```bash
gh pr create --title "feat: add health plan name/number fields to patient form (frontend)" \
  --body "Refs #5

Adds \`healthPlanName\`/\`healthPlanNumber\` to the patient form, Yup schema, and API types, matching the backend fields added in the companion PR. Both optional, placed between Email and Notes in the form. No changes to the patients list table — these stay detail-only, like Notes already is.

Spec: \`docs/superpowers/specs/2026-08-17-patient-health-plan-fields.md\`" \
  --label enhancement --assignee willianbrecher
```

**After this PR merges, close #5 manually** (both its PRs will have landed).
