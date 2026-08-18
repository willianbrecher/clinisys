# CliniSys — Add Health Plan Fields to Patient Spec

Date: 2026-08-17
Status: Draft
Issue: [#5](https://github.com/willianbrecher/clinisys/issues/5)

## 1. Goal

Add two simple, optional text fields to the patient record: health plan **name** and health plan
**number**. Per the issue, this is plain fields on `Patient` — not a separate managed
catalog/list of plans.

## 2. Current state — confirmed

### Backend

`Patient` entity (`backend/src/CliniSys.Domain/Entities/Patient.cs`): `Id`, `FullName`,
`DateOfBirth`, `Phone`, `Email` (nullable), `Notes` (nullable), `IsActive`. No explicit EF Core
column configuration exists for `Patient`'s string fields in `AppDbContext.cs` beyond the primary
key (`builder.Entity<Patient>(e => e.HasKey(p => p.Id));`) — column types/lengths are whatever EF's
default conventions produce; there's only one migration so far
(`Migrations/20260720011310_InitialCreate.cs`).

Every layer threads the same five fields in the same order — `FullName, DateOfBirth, Phone, Email,
Notes` — through:
- `CreatePatientCommand`/`CreatePatientCommandHandler`/`CreatePatientCommandValidator`
  (`FullName`: `NotEmpty().MaximumLength(200)`; `Phone`: `NotEmpty().MaximumLength(30)`; `Email`:
  format-validated only when present; `Notes`: no validation).
- `UpdatePatientCommand`/`Handler`/`Validator` — identical shape and rules.
- `GetPatientsQueryHandler`'s `PatientModel` record (`Id, FullName, DateOfBirth, Phone, Email,
  Notes, IsActive`) — also reused by `GetPatientByIdQueryHandler` (added for #30).
- `CreatePatientRequest`/`UpdatePatientRequest` (API DTOs) — same five fields.
- `PatientsController` — maps `Request` → `Command` field-by-field, no transformation.

### Frontend

- `PatientModel` (`frontend/src/api/types.ts:14-22`) mirrors the backend's `PatientModel` exactly.
- `patientSchema` (`frontend/src/features/patients/patient.schema.ts`) — `fullName` (required,
  max 200), `dateOfBirth` (required), `phone` (required, max 30), `email` (optional, email
  format), `notes` (optional).
- `PatientFormContent.tsx` — one `<Input>`/`<Textarea>` per field, in the same order as the
  schema; `reset()` in the edit-load `useEffect` populates from `getPatientById`'s response the
  same way for every field (`p.email ?? ""` pattern for optional strings).
- `api/patients.ts` — `createPatient`/`updatePatient` accept an inline object type mirroring the
  same five fields.
- `PatientsPage.tsx` — the list table shows `fullName`, `phone`, `email`, actions only (no
  `notes` column) — the list view is intentionally not a full field dump.
- Locale files (`frontend/src/locales/{en-US,pt-BR,es-ES}/translation.json`) — `patients.*` keys:
  `title`, `new`, `fullName`, `dateOfBirth`, `phone`, `email`, `notes`.

## 3. Proposed fix

Two new optional fields, `HealthPlanName`/`HealthPlanNumber` (backend) ↔ `healthPlanName`/
`healthPlanNumber` (frontend), threaded through every layer the same way `Email`/`Notes` already
are — no new abstraction, no separate plan-catalog table (explicitly out of scope per the issue).

### Backend

- `Patient.cs`: add `public string? HealthPlanName { get; set; }` and
  `public string? HealthPlanNumber { get; set; }`, following `Email`/`Notes`'s nullable-string
  pattern.
- `CreatePatientCommand`/`UpdatePatientCommand` (and their handlers): add both fields as trailing
  parameters, mirroring `Notes`'s position (last).
- Validators: add `MaximumLength` rules consistent with the codebase's existing short-text fields —
  `HealthPlanName` capped at 200 (same as `FullName`), `HealthPlanNumber` capped at 50 (a plan
  number is an identifier, not free text; slightly more generous than `Phone`'s 30 to allow for
  longer alphanumeric plan codes). Neither is `NotEmpty()` — both optional, like `Notes`.
- `PatientModel` (in `GetPatientsQueryHandler.cs`, reused by `GetPatientByIdQueryHandler`): add
  both fields; update both handlers' `new PatientModel(...)` calls.
- `CreatePatientRequest`/`UpdatePatientRequest`: add both fields; update `PatientsController`'s
  mapping in `Create`/`Update`.
- EF Core migration: add nullable `text`/`character varying` columns for both new properties —
  generated via `dotnet ef migrations add AddPatientHealthPlanFields`, matching how the single
  existing migration was produced (no other precedent to diverge from; EF's default conventions
  apply, same as every other `Patient` string column).

### Frontend

- `PatientModel` (`api/types.ts`): add `healthPlanName?: string; healthPlanNumber?: string;`.
- `patientSchema`: add `healthPlanName: yup.string().optional()`,
  `healthPlanNumber: yup.string().optional()` — same optional-string shape as `notes`.
- `PatientFormContent.tsx`: two new `<Input>` fields (grid layout, `sm:col-span-1` each, placed
  after `email`/before `notes` — grouping the "administrative" fields together rather than
  splitting them across the form); add both to the edit-load `reset()` call
  (`p.healthPlanName ?? ""`, `p.healthPlanNumber ?? ""`, matching the `email`/`notes` pattern).
- `api/patients.ts`: add both fields to `createPatient`/`updatePatient`'s inline data type.
- Locale bundles: add `patients.healthPlanName`/`patients.healthPlanNumber` keys to all three
  files (`en-US`, `pt-BR`, `es-ES`).

## 4. Non-goals

- No separate health-plan catalog/managed list — explicitly ruled out by the issue itself ("not a
  separate managed list/catalog of plans").
- No change to `PatientsPage.tsx`'s list table — it already omits `notes`, a full-text optional
  field of the same nature; health plan fields follow the same "detail-only, not list-column"
  treatment. Can be revisited later if the user wants it surfaced in the list.
- No format/checksum validation on `healthPlanNumber` (e.g. per-insurer number formats) — plain
  text, per the issue's own field description ("text").
- No change to `DeactivatePatientCommand` or `GetPatientsQuery` (search/filter) — health plan
  fields aren't searchable/filterable in this pass.
