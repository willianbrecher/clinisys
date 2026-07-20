# CliniSys — Backend Design Spec

Date: 2026-07-19
Status: Approved

## 1. Purpose & Scope

A portfolio/learning project: a single-clinic system for registering patients and
doctors and scheduling appointments between them. Used internally by clinic Staff,
Doctors, and an Admin — patients do not log in themselves.

Explicitly out of scope for v1: multi-location support, a patient self-service
portal, email/SMS reminders, recurring appointments, and doctor-specific
availability rules beyond clinic-wide open hours.

## 2. Tech Stack

- **Runtime:** .NET 8, C# 12, nullable enabled, implicit usings, file-scoped namespaces.
- **API:** ASP.NET Core Web API, Swagger/OpenAPI (Swashbuckle).
- **CQRS:** MediatR 12.
- **Validation:** FluentValidation 11 (`FluentValidation.DependencyInjectionExtensions`).
- **Auth:** OpenIddict 5 — OAuth 2.0 server on top of ASP.NET Core Identity + EF Core.
  Handles token issuance, asymmetric signing key generation/rotation, and token
  storage automatically. No manual JWT signing configuration needed.
- **ORM:** Entity Framework Core 8, Npgsql 8.
- **Database:** PostgreSQL 16.

## 3. Architecture

Clean Architecture, 4 projects in `backend/src/`:

```
CliniSys.Domain/          # Entities, enums — no external dependencies
CliniSys.Application/     # CQRS commands/queries, handlers, abstractions, models
CliniSys.Infrastructure/  # EF Core DbContext, migrations, repository implementations, Identity/OpenIddict
CliniSys.Api/             # Controllers, Requests, Swagger, DI wiring, Program.cs
```

Dependency direction: `Api → Application → Domain`, with `Infrastructure`
implementing interfaces defined in `Application`/`Domain`.
Controllers stay thin: each action maps the request model to a Command/Query and
sends it via `IMediator`. No `record`, `class`, or `struct` is defined inside a
controller file.

### CliniSys.Application

Organized by operation type first (`Commands/`, `Queries/`), then by domain area.
Each operation gets its own subfolder with exactly two files — the request record
and its handler — plus an optional validator.

```
CliniSys.Application/
├── Commands/
│   ├── Appointments/
│   │   ├── CreateAppointment/
│   │   │   ├── CreateAppointmentCommand.cs
│   │   │   ├── CreateAppointmentCommandValidator.cs
│   │   │   └── CreateAppointmentCommandHandler.cs
│   │   ├── RescheduleAppointment/
│   │   │   ├── RescheduleAppointmentCommand.cs
│   │   │   ├── RescheduleAppointmentCommandValidator.cs
│   │   │   └── RescheduleAppointmentCommandHandler.cs
│   │   └── UpdateAppointmentStatus/
│   │       ├── UpdateAppointmentStatusCommand.cs
│   │       └── UpdateAppointmentStatusCommandHandler.cs
│   ├── Patients/
│   │   ├── CreatePatient/
│   │   ├── UpdatePatient/
│   │   └── DeactivatePatient/
│   ├── Doctors/
│   │   └── UpdateDoctor/
│   ├── Users/
│   │   ├── CreateUser/
│   │   ├── DeactivateUser/
│   │   └── ResetPassword/
│   ├── Auth/
│   │   └── ChangePassword/
│   ├── Account/
│   │   ├── UpdateProfilePicture/
│   │   └── UpdatePreferences/
│   └── ClinicSettings/
│       └── UpdateClinicSettings/
├── Queries/
│   ├── Appointments/GetAppointments/
│   ├── Patients/GetPatients/
│   ├── Doctors/GetDoctors/
│   ├── Users/GetUsers/
│   └── ClinicSettings/GetClinicSettings/
├── Common/
│   ├── Exceptions/
│   │   ├── NotFoundException.cs
│   │   └── ConflictException.cs
│   ├── Interfaces/
│   │   ├── ICommand.cs
│   │   ├── ICommandHandler.cs
│   │   ├── IQuery.cs
│   │   ├── IQueryHandler.cs
│   │   ├── IPagedQuery.cs
│   │   ├── IIdentityService.cs
│   │   └── Repositories/
│   │       ├── IRepository.cs
│   │       ├── IAppointmentRepository.cs
│   │       ├── IPatientRepository.cs
│   │       ├── IDoctorRepository.cs
│   │       ├── IUserRepository.cs
│   │       └── IClinicSettingsRepository.cs
│   └── Models/
│       └── PagedResult.cs
└── Behaviours/
    └── ValidationBehaviour.cs
```

**CQRS rules:**
- Every command `record` implements `ICommand<TResult>` (not `IRequest<T>` directly).
- Every query `record` implements `IQuery<TResult>` or `IPagedQuery<TItem>`.
- Every command handler implements `ICommandHandler<TCommand, TResult>`.
- Every query handler implements `IQueryHandler<TQuery, TResult>`.
- `*Command.cs` / `*Query.cs` contains only the `record` definition. No logic.
- `*CommandHandler.cs` / `*QueryHandler.cs` contains the handler class, plus any
  single-use response model `record` defined at the top of the same file.
- `*CommandValidator.cs` lives alongside the command when validation is needed.

**Pagination:**

`PagedResult<T>` is the standard response envelope for all list endpoints:
```csharp
record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
```

List queries implement `IPagedQuery<TItem>`. The query record always includes
`Page` (default 1) and `PageSize` (default 20, max 100). `PageSize` above 100 is
rejected with `400` by the FluentValidation pipeline. Repository methods return
`PagedResult<T>` directly — handlers never call `.Skip`/`.Take` themselves.

**Repository pattern:**

`IRepository<T>` is the generic base:
```csharp
interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

Entity-specific interfaces:
- `IAppointmentRepository` — `GetByDoctorAndDateAsync(Guid, DateOnly)`, `GetPagedAsync(filters…)`
- `IPatientRepository` — `GetPagedAsync(string? search, page, pageSize)`
- `IDoctorRepository` — `GetPagedAsync(page, pageSize)`, `GetByUserIdAsync(Guid userId)`
- `IUserRepository` — `GetByEmailAsync(string email)`, `GetPagedAsync(page, pageSize)`
- `IClinicSettingsRepository` — `GetSingletonAsync()`

Rules:
- Handlers inject only repository interfaces and `IIdentityService` — never `AppDbContext`,
  `DbSet<T>`, `UserManager`, or any EF/Identity type directly.
- All EF Core stays inside `CliniSys.Infrastructure`.
- `SaveChangesAsync` is called by the handler after all mutations; repositories do not auto-save.

**IIdentityService** (Application interface, Infrastructure implementation):
```csharp
interface IIdentityService
{
    Task<Guid> CreateUserAsync(string email, string fullName, string password, Role role);
    Task ResetPasswordAsync(Guid userId, string newPassword);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task DeactivateUserAsync(Guid userId);
}
```

### CliniSys.Infrastructure

```
CliniSys.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Migrations/
│   └── Repositories/
│       ├── Repository.cs
│       ├── AppointmentRepository.cs
│       ├── PatientRepository.cs
│       ├── DoctorRepository.cs
│       ├── UserRepository.cs
│       └── ClinicSettingsRepository.cs
├── Identity/
│   └── IdentityService.cs          # IIdentityService implementation using UserManager
└── DependencyInjection.cs
```

**OpenIddict — authentication & token management:**

- Grant type: Resource Owner Password Credentials (`grant_type=password`).
  Frontend POSTs `username`, `password`, `scope` to `POST /connect/token`.
- Signing keys: generated automatically by OpenIddict on first startup; stored in DB.
- Custom claims added at token issuance: `role`, `theme`, `language`, `doctorId` (Doctor only).
- Token storage: four OpenIddict tables added via `options.UseEntityFrameworkCore()`.
- Validation: OpenIddict's ASP.NET Core middleware validates tokens on every request.
- `POST /connect/token` is handled by a passthrough controller action, NOT MediatR.

### CliniSys.Api

```
CliniSys.Api/
├── Controllers/
│   ├── ConnectController.cs         # OpenIddict token endpoint passthrough
│   ├── AppointmentsController.cs
│   ├── PatientsController.cs
│   ├── DoctorsController.cs
│   ├── UsersController.cs
│   ├── AuthController.cs
│   ├── AccountController.cs
│   └── ClinicSettingsController.cs
├── Middleware/
│   └── ExceptionMiddleware.cs
├── Requests/
│   ├── Appointments/
│   │   ├── CreateAppointmentRequest.cs
│   │   ├── RescheduleAppointmentRequest.cs
│   │   └── UpdateAppointmentStatusRequest.cs
│   ├── Patients/
│   │   ├── CreatePatientRequest.cs
│   │   └── UpdatePatientRequest.cs
│   ├── Doctors/
│   │   └── UpdateDoctorRequest.cs
│   ├── Users/
│   │   ├── CreateUserRequest.cs
│   │   └── ResetPasswordRequest.cs
│   ├── Auth/
│   │   └── ChangePasswordRequest.cs
│   ├── Account/
│   │   ├── UpdateProfilePictureRequest.cs
│   │   └── UpdatePreferencesRequest.cs
│   └── ClinicSettings/
│       └── UpdateClinicSettingsRequest.cs
└── Program.cs
```

## 4. Domain Model

**ApplicationUser** (extends `IdentityUser<Guid>`)
- `FullName`, `Role` (`Admin|Staff|Doctor`),
  `ProfilePictureBase64` (nullable data URI),
  `ThemePreference` (`Light|Dark|System`, default `System`),
  `LanguagePreference` (`en-US|pt-BR|es-ES`, default `en-US`)

**Doctor**
- `Id`, `UserId` (FK → ApplicationUser, 1:1), `Specialty`, `IsActive`

**Patient** (no login)
- `Id`, `FullName`, `DateOfBirth`, `Phone`, `Email` (optional), `Notes` (optional), `IsActive`

**Appointment**
- `Id`, `PatientId` (FK), `DoctorId` (FK), `StartsAt` (UTC datetime),
  `DurationMinutes`, `Status` (`Scheduled|Confirmed|Completed|Cancelled|NoShow`),
  `Notes` (optional), `CreatedAt`

**ClinicSettings** (single row, admin-configurable)
- `Id`, `OpenTime` (TimeOnly), `CloseTime` (TimeOnly),
  `OpenDays` (comma-separated weekday ints, e.g. `"1,2,3,4,5"`),
  `LogoBase64` (nullable data URI)

### Business Rules

- Booking/rescheduling: `StartsAt..StartsAt+DurationMinutes` must fall within
  `ClinicSettings` open hours/days AND must not overlap any non-cancelled appointment
  for that `DoctorId`. Both checks done in the handler (load appointments for the
  day via repository, check overlap in memory).
- Status transitions: restricted set — e.g. cannot go from `Completed` back to
  `Scheduled`. Enforced in `UpdateAppointmentStatusCommandHandler`.
- Patient/Doctor "deletion" is soft-delete via `IsActive = false`.
- Images (logo, profile picture) stored as base64 data URI strings, max 512 KB
  (enforced in FluentValidation via byte-count estimation on the base64 string).

## 5. API Surface

**Token (OpenIddict — not a controller action)**
- `POST /connect/token` — `grant_type=password&username=&password=&scope=openid`
  Returns `{ access_token, token_type, expires_in }`. JWT includes custom claims:
  `role`, `theme`, `language`, `doctorId` (Doctor role only).

**Auth**
- `POST /api/auth/change-password` — `ChangePasswordCommand`, authenticated user
  changes their own password.

**Users** (Admin only)
- `POST /api/users` — `CreateUserCommand` (if role=Doctor, also creates Doctor profile)
- `GET /api/users?page=&pageSize=` — `GetUsersQuery` → `PagedResult<UserModel>`
- `PATCH /api/users/{id}/deactivate` — `DeactivateUserCommand`
- `POST /api/users/{id}/reset-password` — `ResetPasswordCommand`

**Doctors**
- `GET /api/doctors?page=&pageSize=` — `GetDoctorsQuery` → `PagedResult<DoctorModel>`
- `GET /api/doctors/{id}` — returns single `DoctorModel`
- `PATCH /api/doctors/{id}` — `UpdateDoctorCommand` (Admin only)

**Patients** (Staff/Admin)
- `POST /api/patients` — `CreatePatientCommand`
- `GET /api/patients?search=&page=&pageSize=` — `GetPatientsQuery` → `PagedResult<PatientModel>`
- `GET /api/patients/{id}` — returns single `PatientModel`
- `PUT /api/patients/{id}` — `UpdatePatientCommand`
- `DELETE /api/patients/{id}` — `DeactivatePatientCommand` (soft delete)

**Appointments**
- `POST /api/appointments` — `CreateAppointmentCommand`
- `GET /api/appointments?doctorId=&patientId=&date=&startDate=&endDate=&status=&page=&pageSize=`
  → `PagedResult<AppointmentModel>`. When `startDate`+`endDate` provided, pagination is
  ignored and all matching records are returned (calendar view).
- `PUT /api/appointments/{id}` — `RescheduleAppointmentCommand`
- `PATCH /api/appointments/{id}/status` — `UpdateAppointmentStatusCommand`

Doctor callers: `GetAppointmentsQuery` filtered to `doctorId == currentUser.DoctorId`
(controller reads `doctorId` claim and passes it as a filter).

**Clinic Settings**
- `GET /api/clinic-settings` — all authenticated roles
- `PUT /api/clinic-settings` — `UpdateClinicSettingsCommand` (Admin only)

**Account** (all authenticated roles)
- `PATCH /api/account/profile-picture` — `UpdateProfilePictureCommand`
- `PATCH /api/account/preferences` — `UpdatePreferencesCommand`

## 6. Error Handling

Global exception-handling middleware maps exceptions to JSON (`{ message, errors? }`):
- `400` — `ValidationException` (FluentValidation)
- `404` — `NotFoundException` (thrown in handlers, defined in Application)
- `409` — `ConflictException` (double-booking etc., defined in Application)
- `500` — unhandled, logged server-side, generic message to client

`NotFoundException` and `ConflictException` live in
`CliniSys.Application/Common/Exceptions/` so handlers can throw them without
depending on the Api project.

## 7. Internationalization (Backend)

- `LocalizationMiddleware` reads the `Accept-Language` request header and sets
  `CultureInfo.CurrentCulture` / `CurrentUICulture` before the MVC pipeline runs.
- `IMessageLocalizer` interface in Application; concrete implementation loads
  messages from JSON files at `CliniSys.Application/Locales/<locale>.json`
  (keys match the frontend translation files).
- FluentValidation error messages use `.WithMessage()` with a localized string
  via the injected `IMessageLocalizer`.
- Supported locales: `en-US` (default), `pt-BR`, `es-ES`.
- API field names and route paths always stay in English; only human-readable
  `message` / `errors[*].message` strings are localized.

## 8. XML Documentation

All public types and members in `CliniSys.Domain`, `CliniSys.Application`, and
`CliniSys.Api` must have XML doc comments. Minimum: `<summary>`, `<param>`,
`<returns>`. Infrastructure is exempt.

`CliniSys.Api.csproj`: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`.
`AddSwaggerGen` wired with `IncludeXmlComments(xmlPath)`.

## 9. Testing

No dedicated test project for v1. Handlers depend only on repository interfaces
and `IIdentityService`, so unit tests can be added later without restructuring.
