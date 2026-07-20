# CliniSys Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the CliniSys .NET 8 REST API — patient/doctor/appointment scheduling with CQRS, OpenIddict auth, repository pattern, and PostgreSQL.

**Architecture:** Clean Architecture (Domain → Application → Infrastructure → Api). MediatR CQRS handlers talk to repositories only. Controllers map request models to commands/queries.

**Tech Stack:** .NET 8, C# 12, MediatR 12, FluentValidation 11, EF Core 8 + Npgsql 8, OpenIddict 5, ASP.NET Core Identity, Swashbuckle 6, PostgreSQL 16.

## Global Constraints

- Target `net8.0`; `<Nullable>enable</Nullable>`; `<LangVersion>12.0</LangVersion>`; file-scoped namespaces everywhere
- XML doc `<summary>`, `<param>`, `<returns>` on every public type/member in Domain, Application, Api — Infrastructure is exempt
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` on Domain, Application, Api projects
- No `record`/`class`/`struct` defined inside a controller file — all in `Requests/`
- Handlers inject only repository interfaces and `IIdentityService` — never `AppDbContext`, `UserManager`, or EF types
- `SaveChangesAsync` called by the handler; repositories never auto-save
- `PageSize` above 100 rejected via FluentValidation → HTTP 400
- Soft delete only: `IsActive = false` — never `DbContext.Remove()` for Patient or Doctor
- Images stored as base64 data URI strings; max 512 KB checked by byte-count estimate on the base64 string
- No test project for v1 (handlers are isolated; tests can be added later)

---

### Task 1: Solution Scaffold

**Files:**
- Create: `backend/CliniSys.sln`
- Create: `backend/src/CliniSys.Domain/CliniSys.Domain.csproj`
- Create: `backend/src/CliniSys.Application/CliniSys.Application.csproj`
- Create: `backend/src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj`
- Create: `backend/src/CliniSys.Api/CliniSys.Api.csproj`

**Interfaces:**
- Produces: buildable solution; all NuGet packages installed; project references wired

- [ ] **Step 1: Scaffold solution and projects**

```bash
mkdir -p backend/src
cd backend
dotnet new sln -n CliniSys
dotnet new classlib -n CliniSys.Domain         -o src/CliniSys.Domain         --framework net8.0
dotnet new classlib -n CliniSys.Application    -o src/CliniSys.Application    --framework net8.0
dotnet new classlib -n CliniSys.Infrastructure -o src/CliniSys.Infrastructure --framework net8.0
dotnet new webapi   -n CliniSys.Api            -o src/CliniSys.Api            --framework net8.0
dotnet sln add src/CliniSys.Domain/CliniSys.Domain.csproj
dotnet sln add src/CliniSys.Application/CliniSys.Application.csproj
dotnet sln add src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj
dotnet sln add src/CliniSys.Api/CliniSys.Api.csproj
```

- [ ] **Step 2: Add project references**

```bash
cd backend
dotnet add src/CliniSys.Application/CliniSys.Application.csproj       reference src/CliniSys.Domain/CliniSys.Domain.csproj
dotnet add src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj  reference src/CliniSys.Application/CliniSys.Application.csproj
dotnet add src/CliniSys.Api/CliniSys.Api.csproj                        reference src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj
dotnet add src/CliniSys.Api/CliniSys.Api.csproj                        reference src/CliniSys.Application/CliniSys.Application.csproj
```

- [ ] **Step 3: Install NuGet packages**

```bash
cd backend
dotnet add src/CliniSys.Application/CliniSys.Application.csproj       package MediatR --version 12.*
dotnet add src/CliniSys.Application/CliniSys.Application.csproj       package FluentValidation.DependencyInjectionExtensions --version 11.*

dotnet add src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.*
dotnet add src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.*
dotnet add src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj package OpenIddict.AspNetCore --version 5.*
dotnet add src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj package OpenIddict.EntityFrameworkCore --version 5.*
dotnet add src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design --version 8.*

dotnet add src/CliniSys.Api/CliniSys.Api.csproj                       package Swashbuckle.AspNetCore --version 6.*
```

- [ ] **Step 4: Replace csproj files**

`backend/src/CliniSys.Domain/CliniSys.Domain.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>12.0</LangVersion>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

`backend/src/CliniSys.Application/CliniSys.Application.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>12.0</LangVersion>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\CliniSys.Domain\CliniSys.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.*" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.*" />
  </ItemGroup>
</Project>
```

`backend/src/CliniSys.Infrastructure/CliniSys.Infrastructure.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>12.0</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\CliniSys.Application\CliniSys.Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.*" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.*" />
    <PackageReference Include="OpenIddict.AspNetCore" Version="5.*" />
    <PackageReference Include="OpenIddict.EntityFrameworkCore" Version="5.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

`backend/src/CliniSys.Api/CliniSys.Api.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>12.0</LangVersion>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\CliniSys.Infrastructure\CliniSys.Infrastructure.csproj" />
    <ProjectReference Include="..\CliniSys.Application\CliniSys.Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Delete boilerplate**

```bash
rm -f backend/src/CliniSys.Domain/Class1.cs
rm -f backend/src/CliniSys.Application/Class1.cs
rm -f backend/src/CliniSys.Infrastructure/Class1.cs
rm -f backend/src/CliniSys.Api/Controllers/WeatherForecastController.cs
rm -f backend/src/CliniSys.Api/WeatherForecast.cs
```

- [ ] **Step 6: Verify build**

```bash
cd backend && dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add backend/
git commit -m "chore: scaffold CliniSys solution with 4 projects and NuGet packages"
```

---

### Task 2: Domain Layer

**Files:**
- Create: `backend/src/CliniSys.Domain/Enums/Role.cs`
- Create: `backend/src/CliniSys.Domain/Enums/AppointmentStatus.cs`
- Create: `backend/src/CliniSys.Domain/Enums/ThemePreference.cs`
- Create: `backend/src/CliniSys.Domain/Entities/ApplicationUser.cs`
- Create: `backend/src/CliniSys.Domain/Entities/Doctor.cs`
- Create: `backend/src/CliniSys.Domain/Entities/Patient.cs`
- Create: `backend/src/CliniSys.Domain/Entities/Appointment.cs`
- Create: `backend/src/CliniSys.Domain/Entities/ClinicSettings.cs`

**Interfaces:**
- Produces: all entity types and enums used by Application and Infrastructure

- [ ] **Step 1: Create enums**

`backend/src/CliniSys.Domain/Enums/Role.cs`:
```csharp
namespace CliniSys.Domain.Enums;

/// <summary>User roles in the clinic system.</summary>
public enum Role
{
    /// <summary>System administrator with full access.</summary>
    Admin,
    /// <summary>Front-desk staff for scheduling and patient management.</summary>
    Staff,
    /// <summary>A medical doctor linked to a Doctor profile.</summary>
    Doctor
}
```

`backend/src/CliniSys.Domain/Enums/AppointmentStatus.cs`:
```csharp
namespace CliniSys.Domain.Enums;

/// <summary>Lifecycle states for an appointment.</summary>
public enum AppointmentStatus
{
    /// <summary>Booked but not yet confirmed.</summary>
    Scheduled,
    /// <summary>Confirmed by doctor or staff.</summary>
    Confirmed,
    /// <summary>Appointment has taken place.</summary>
    Completed,
    /// <summary>Cancelled before the appointment.</summary>
    Cancelled,
    /// <summary>Patient did not show up.</summary>
    NoShow
}
```

`backend/src/CliniSys.Domain/Enums/ThemePreference.cs`:
```csharp
namespace CliniSys.Domain.Enums;

/// <summary>UI theme preference for a user.</summary>
public enum ThemePreference
{
    /// <summary>Always use light mode.</summary>
    Light,
    /// <summary>Always use dark mode.</summary>
    Dark,
    /// <summary>Follow the operating system preference.</summary>
    System
}
```

- [ ] **Step 2: Create ApplicationUser**

`backend/src/CliniSys.Domain/Entities/ApplicationUser.cs`:
```csharp
using CliniSys.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CliniSys.Domain.Entities;

/// <summary>A system user who can log in. Backed by ASP.NET Core Identity.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Full display name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Role controlling access within the system.</summary>
    public Role Role { get; set; }

    /// <summary>Optional profile picture as a base64 data URI (e.g. <c>data:image/png;base64,...</c>).</summary>
    public string? ProfilePictureBase64 { get; set; }

    /// <summary>Preferred colour theme. Defaults to <see cref="ThemePreference.System"/>.</summary>
    public ThemePreference ThemePreference { get; set; } = ThemePreference.System;

    /// <summary>BCP-47 language tag. Supported: <c>en-US</c>, <c>pt-BR</c>, <c>es-ES</c>. Defaults to <c>en-US</c>.</summary>
    public string LanguagePreference { get; set; } = "en-US";
}
```

- [ ] **Step 3: Create Doctor, Patient, Appointment, ClinicSettings**

`backend/src/CliniSys.Domain/Entities/Doctor.cs`:
```csharp
namespace CliniSys.Domain.Entities;

/// <summary>Doctor profile linked 1:1 to an <see cref="ApplicationUser"/> with role Doctor.</summary>
public class Doctor
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }
    /// <summary>FK to the associated user.</summary>
    public Guid UserId { get; set; }
    /// <summary>Navigation property to the user.</summary>
    public ApplicationUser User { get; set; } = null!;
    /// <summary>Free-form medical specialty (e.g. "Cardiology").</summary>
    public string Specialty { get; set; } = string.Empty;
    /// <summary>False when soft-deleted.</summary>
    public bool IsActive { get; set; } = true;
}
```

`backend/src/CliniSys.Domain/Entities/Patient.cs`:
```csharp
namespace CliniSys.Domain.Entities;

/// <summary>A clinic patient. Patients do not have login accounts.</summary>
public class Patient
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }
    /// <summary>Full name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Date of birth.</summary>
    public DateOnly DateOfBirth { get; set; }
    /// <summary>Contact phone number.</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>Optional contact email.</summary>
    public string? Email { get; set; }
    /// <summary>Optional notes (insurance, medical, etc.).</summary>
    public string? Notes { get; set; }
    /// <summary>False when soft-deleted.</summary>
    public bool IsActive { get; set; } = true;
}
```

`backend/src/CliniSys.Domain/Entities/Appointment.cs`:
```csharp
using CliniSys.Domain.Enums;

namespace CliniSys.Domain.Entities;

/// <summary>An appointment scheduled between a patient and a doctor.</summary>
public class Appointment
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }
    /// <summary>FK to the patient.</summary>
    public Guid PatientId { get; set; }
    /// <summary>Navigation property to the patient.</summary>
    public Patient Patient { get; set; } = null!;
    /// <summary>FK to the doctor.</summary>
    public Guid DoctorId { get; set; }
    /// <summary>Navigation property to the doctor.</summary>
    public Doctor Doctor { get; set; } = null!;
    /// <summary>UTC date and time the appointment starts.</summary>
    public DateTime StartsAt { get; set; }
    /// <summary>Duration in minutes.</summary>
    public int DurationMinutes { get; set; }
    /// <summary>Current lifecycle status.</summary>
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    /// <summary>Optional appointment notes.</summary>
    public string? Notes { get; set; }
    /// <summary>UTC timestamp when created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

`backend/src/CliniSys.Domain/Entities/ClinicSettings.cs`:
```csharp
namespace CliniSys.Domain.Entities;

/// <summary>Singleton row storing clinic-wide configuration.</summary>
public class ClinicSettings
{
    /// <summary>Primary key (only one row exists).</summary>
    public Guid Id { get; set; }
    /// <summary>Time the clinic opens each working day.</summary>
    public TimeOnly OpenTime { get; set; }
    /// <summary>Time the clinic closes each working day.</summary>
    public TimeOnly CloseTime { get; set; }
    /// <summary>Comma-separated weekday numbers, 0=Sun…6=Sat (e.g. <c>"1,2,3,4,5"</c>).</summary>
    public string OpenDays { get; set; } = "1,2,3,4,5";
    /// <summary>Optional clinic logo as a base64 data URI. <see langword="null"/> means no logo.</summary>
    public string? LogoBase64 { get; set; }
}
```

- [ ] **Step 4: Verify build**

```bash
cd backend && dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add backend/src/CliniSys.Domain/
git commit -m "feat: add Domain entities and enums"
```

---

### Task 3: Application — CQRS Abstractions, Exceptions, PagedResult

**Files:**
- Create: `backend/src/CliniSys.Application/Common/Interfaces/ICommand.cs`
- Create: `backend/src/CliniSys.Application/Common/Interfaces/ICommandHandler.cs`
- Create: `backend/src/CliniSys.Application/Common/Interfaces/IQuery.cs`
- Create: `backend/src/CliniSys.Application/Common/Interfaces/IQueryHandler.cs`
- Create: `backend/src/CliniSys.Application/Common/Interfaces/IPagedQuery.cs`
- Create: `backend/src/CliniSys.Application/Common/Models/PagedResult.cs`
- Create: `backend/src/CliniSys.Application/Common/Exceptions/NotFoundException.cs`
- Create: `backend/src/CliniSys.Application/Common/Exceptions/ConflictException.cs`

**Interfaces:**
- Produces: all CQRS marker interfaces and `PagedResult<T>` used by every feature task

- [ ] **Step 1: Create CQRS interfaces**

`backend/src/CliniSys.Application/Common/Interfaces/ICommand.cs`:
```csharp
using MediatR;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>Marker interface for a CQRS command returning <typeparamref name="TResult"/>.</summary>
/// <typeparam name="TResult">The handler return type.</typeparam>
public interface ICommand<TResult> : IRequest<TResult> { }
```

`backend/src/CliniSys.Application/Common/Interfaces/ICommandHandler.cs`:
```csharp
using MediatR;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>Marker interface for a CQRS command handler.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult> { }
```

`backend/src/CliniSys.Application/Common/Interfaces/IQuery.cs`:
```csharp
using MediatR;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>Marker interface for a CQRS query returning <typeparamref name="TResult"/>.</summary>
/// <typeparam name="TResult">The handler return type.</typeparam>
public interface IQuery<TResult> : IRequest<TResult> { }
```

`backend/src/CliniSys.Application/Common/Interfaces/IQueryHandler.cs`:
```csharp
using MediatR;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>Marker interface for a CQRS query handler.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult> { }
```

`backend/src/CliniSys.Application/Common/Interfaces/IPagedQuery.cs`:
```csharp
using CliniSys.Application.Common.Models;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>Marker interface for a pageable list query.</summary>
/// <typeparam name="TItem">The type of each result item.</typeparam>
public interface IPagedQuery<TItem> : IQuery<PagedResult<TItem>>
{
    /// <summary>1-based page number.</summary>
    int Page { get; }
    /// <summary>Items per page (max 100).</summary>
    int PageSize { get; }
}
```

`backend/src/CliniSys.Application/Common/Models/PagedResult.cs`:
```csharp
namespace CliniSys.Application.Common.Models;

/// <summary>Standard response envelope for paginated list endpoints.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">Items on the current page.</param>
/// <param name="Page">1-based current page number.</param>
/// <param name="PageSize">Requested page size.</param>
/// <param name="TotalCount">Total matching items across all pages.</param>
/// <param name="TotalPages">Total number of pages available.</param>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
```

- [ ] **Step 2: Create domain exceptions**

`backend/src/CliniSys.Application/Common/Exceptions/NotFoundException.cs`:
```csharp
namespace CliniSys.Application.Common.Exceptions;

/// <summary>Thrown when a requested resource does not exist. Maps to HTTP 404.</summary>
/// <param name="message">Human-readable description of the missing resource.</param>
public class NotFoundException(string message) : Exception(message);
```

`backend/src/CliniSys.Application/Common/Exceptions/ConflictException.cs`:
```csharp
namespace CliniSys.Application.Common.Exceptions;

/// <summary>Thrown when a request conflicts with current state (e.g. double-booking). Maps to HTTP 409.</summary>
/// <param name="message">Human-readable description of the conflict.</param>
public class ConflictException(string message) : Exception(message);
```

- [ ] **Step 3: Verify build**

```bash
cd backend && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add backend/src/CliniSys.Application/Common/
git commit -m "feat: add CQRS abstractions, PagedResult, and domain exceptions"
```

---

### Task 4: Repository Interfaces + IIdentityService

**Files:**
- Create: `backend/src/CliniSys.Application/Common/Interfaces/Repositories/IRepository.cs`
- Create: `backend/src/CliniSys.Application/Common/Interfaces/Repositories/IAppointmentRepository.cs`
- Create: `backend/src/CliniSys.Application/Common/Interfaces/Repositories/IPatientRepository.cs`
- Create: `backend/src/CliniSys.Application/Common/Interfaces/Repositories/IDoctorRepository.cs`
- Create: `backend/src/CliniSys.Application/Common/Interfaces/Repositories/IUserRepository.cs`
- Create: `backend/src/CliniSys.Application/Common/Interfaces/Repositories/IClinicSettingsRepository.cs`
- Create: `backend/src/CliniSys.Application/Common/Interfaces/IIdentityService.cs`

**Interfaces:**
- Produces: all repository contracts and `IIdentityService` used by handlers; implemented in Task 6

- [ ] **Step 1: Create IRepository**

`backend/src/CliniSys.Application/Common/Interfaces/Repositories/IRepository.cs`:
```csharp
namespace CliniSys.Application.Common.Interfaces.Repositories;

/// <summary>Generic CRUD repository base.</summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Finds an entity by primary key. Returns <see langword="null"/> if not found.</summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Stages a new entity for insert (call <see cref="SaveChangesAsync"/> to persist).</summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(T entity, CancellationToken ct = default);

    /// <summary>Marks an entity as modified (call <see cref="SaveChangesAsync"/> to persist).</summary>
    /// <param name="entity">The entity to update.</param>
    void Update(T entity);

    /// <summary>Marks an entity for removal (call <see cref="SaveChangesAsync"/> to persist).</summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(T entity);

    /// <summary>Persists all staged changes to the database.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Create entity-specific repository interfaces**

`backend/src/CliniSys.Application/Common/Interfaces/Repositories/IAppointmentRepository.cs`:
```csharp
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Common.Interfaces.Repositories;

/// <summary>Repository for <see cref="Appointment"/> with filtering and overlap-check support.</summary>
public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>Returns all non-cancelled appointments for a doctor on a specific date (for overlap validation).</summary>
    /// <param name="doctorId">Doctor identifier.</param>
    /// <param name="date">The date to check.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<Appointment>> GetByDoctorAndDateAsync(Guid doctorId, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated list of appointments. When both <paramref name="startDate"/> and
    /// <paramref name="endDate"/> are provided, pagination is ignored and all matching records are returned.
    /// </summary>
    /// <param name="doctorId">Optional doctor filter.</param>
    /// <param name="patientId">Optional patient filter.</param>
    /// <param name="date">Optional single-day filter.</param>
    /// <param name="startDate">Optional range start (calendar view).</param>
    /// <param name="endDate">Optional range end (calendar view).</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated appointments.</returns>
    Task<PagedResult<Appointment>> GetPagedAsync(
        Guid? doctorId, Guid? patientId, DateOnly? date,
        DateTime? startDate, DateTime? endDate, AppointmentStatus? status,
        int page, int pageSize, CancellationToken ct = default);
}
```

`backend/src/CliniSys.Application/Common/Interfaces/Repositories/IPatientRepository.cs`:
```csharp
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;

namespace CliniSys.Application.Common.Interfaces.Repositories;

/// <summary>Repository for <see cref="Patient"/> with name-search support.</summary>
public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>Returns paginated active patients, optionally filtered by name substring.</summary>
    /// <param name="search">Optional case-insensitive name filter.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PagedResult<Patient>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct = default);
}
```

`backend/src/CliniSys.Application/Common/Interfaces/Repositories/IDoctorRepository.cs`:
```csharp
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;

namespace CliniSys.Application.Common.Interfaces.Repositories;

/// <summary>Repository for <see cref="Doctor"/> with pagination and user-link lookup.</summary>
public interface IDoctorRepository : IRepository<Doctor>
{
    /// <summary>Returns paginated active doctors (includes User navigation).</summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PagedResult<Doctor>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);

    /// <summary>Finds the doctor profile linked to a user. Returns <see langword="null"/> if none.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Doctor?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
```

`backend/src/CliniSys.Application/Common/Interfaces/Repositories/IUserRepository.cs`:
```csharp
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;

namespace CliniSys.Application.Common.Interfaces.Repositories;

/// <summary>Repository for <see cref="ApplicationUser"/> with email lookup and pagination.</summary>
public interface IUserRepository : IRepository<ApplicationUser>
{
    /// <summary>Finds a user by email. Returns <see langword="null"/> if not found.</summary>
    /// <param name="email">Email address to search.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Returns paginated list of all users.</summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PagedResult<ApplicationUser>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
}
```

`backend/src/CliniSys.Application/Common/Interfaces/Repositories/IClinicSettingsRepository.cs`:
```csharp
using CliniSys.Domain.Entities;

namespace CliniSys.Application.Common.Interfaces.Repositories;

/// <summary>Repository for the singleton <see cref="ClinicSettings"/> row.</summary>
public interface IClinicSettingsRepository : IRepository<ClinicSettings>
{
    /// <summary>Returns the single clinic settings row, creating a default one if it does not exist.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="ClinicSettings"/> instance.</returns>
    Task<ClinicSettings> GetSingletonAsync(CancellationToken ct = default);
}
```

- [ ] **Step 3: Create IIdentityService**

`backend/src/CliniSys.Application/Common/Interfaces/IIdentityService.cs`:
```csharp
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>
/// Abstraction over ASP.NET Core Identity operations used by Application handlers.
/// Keeps handlers free from <c>UserManager</c> and Identity SDK types.
/// </summary>
public interface IIdentityService
{
    /// <summary>Creates a new user account with the given password and role.</summary>
    /// <param name="email">Email address (also used as username).</param>
    /// <param name="fullName">Display name.</param>
    /// <param name="password">Plain-text password (hashed by Identity).</param>
    /// <param name="role">The user's role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new user's <see cref="Guid"/> identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Identity reports errors.</exception>
    Task<Guid> CreateUserAsync(string email, string fullName, string password, Role role, CancellationToken ct = default);

    /// <summary>Resets a user's password to a new value (admin action — no current password required).</summary>
    /// <param name="userId">Target user identifier.</param>
    /// <param name="newPassword">The new plain-text password.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);

    /// <summary>Changes the calling user's own password (requires current password).</summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="currentPassword">Current plain-text password for verification.</param>
    /// <param name="newPassword">New plain-text password.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>Locks a user out indefinitely (soft deactivation).</summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeactivateUserAsync(Guid userId, CancellationToken ct = default);
}
```

- [ ] **Step 4: Verify build**

```bash
cd backend && dotnet build
```

- [ ] **Step 5: Commit**

```bash
git add backend/src/CliniSys.Application/Common/Interfaces/
git commit -m "feat: add repository interfaces and IIdentityService"
```

---

### Task 5: ValidationBehaviour + Application DI

**Files:**
- Create: `backend/src/CliniSys.Application/Behaviours/ValidationBehaviour.cs`
- Create: `backend/src/CliniSys.Application/DependencyInjection.cs`

**Interfaces:**
- Produces: MediatR pipeline that runs all validators before every handler; `AddApplication()` service extension

- [ ] **Step 1: Create ValidationBehaviour**

`backend/src/CliniSys.Application/Behaviours/ValidationBehaviour.cs`:
```csharp
using FluentValidation;
using MediatR;

namespace CliniSys.Application.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs all registered FluentValidation validators for the
/// request before the handler executes. Throws <see cref="ValidationException"/> on failure;
/// the API exception middleware maps this to HTTP 400.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>Initialises the behaviour with all DI-registered validators for <typeparamref name="TRequest"/>.</summary>
    /// <param name="validators">Resolved validators.</param>
    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators) =>
        _validators = validators;

    /// <summary>Validates the request, then calls the next handler in the pipeline.</summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">Delegate to the next pipeline step.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The handler result.</returns>
    /// <exception cref="ValidationException">Thrown when any validator reports failures.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count > 0) throw new ValidationException(failures);

        return await next();
    }
}
```

- [ ] **Step 2: Create Application DI extension**

`backend/src/CliniSys.Application/DependencyInjection.cs`:
```csharp
using CliniSys.Application.Behaviours;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CliniSys.Application;

/// <summary>Registers all Application-layer services.</summary>
public static class DependencyInjection
{
    /// <summary>Adds MediatR, FluentValidation validators, and the validation pipeline behaviour.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        return services;
    }
}
```

- [ ] **Step 3: Verify build**

```bash
cd backend && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add backend/src/CliniSys.Application/Behaviours/ backend/src/CliniSys.Application/DependencyInjection.cs
git commit -m "feat: add ValidationBehaviour and Application DI extension"
```

---

### Task 6: AppDbContext + Infrastructure DI + Repository Stubs

**Files:**
- Create: `backend/src/CliniSys.Infrastructure/Persistence/AppDbContext.cs`
- Create: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/Repository.cs`
- Create: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/AppointmentRepository.cs`
- Create: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/PatientRepository.cs`
- Create: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/DoctorRepository.cs`
- Create: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/UserRepository.cs`
- Create: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/ClinicSettingsRepository.cs`
- Create: `backend/src/CliniSys.Infrastructure/Identity/IdentityService.cs`
- Create: `backend/src/CliniSys.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Produces: compilable Infrastructure project; all interfaces wired to stub implementations; `AddInfrastructure(connectionString)` DI extension

- [ ] **Step 1: Create AppDbContext**

`backend/src/CliniSys.Infrastructure/Persistence/AppDbContext.cs`:
```csharp
using CliniSys.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence;

/// <summary>
/// EF Core database context. Extends Identity and includes OpenIddict token tables.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>Initialises the context with the provided options.</summary>
    /// <param name="options">EF Core options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Doctor profiles.</summary>
    public DbSet<Doctor> Doctors => Set<Doctor>();
    /// <summary>Patient records.</summary>
    public DbSet<Patient> Patients => Set<Patient>();
    /// <summary>Scheduled appointments.</summary>
    public DbSet<Appointment> Appointments => Set<Appointment>();
    /// <summary>Singleton clinic configuration.</summary>
    public DbSet<ClinicSettings> ClinicSettings => Set<ClinicSettings>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Doctor>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasOne(d => d.User).WithOne()
             .HasForeignKey<Doctor>(d => d.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Patient>(e => e.HasKey(p => p.Id));

        builder.Entity<Appointment>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasOne(a => a.Patient).WithMany()
             .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Doctor).WithMany()
             .HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClinicSettings>(e => e.HasKey(s => s.Id));
    }
}
```

- [ ] **Step 2: Create base Repository**

`backend/src/CliniSys.Infrastructure/Persistence/Repositories/Repository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext context) { _context = context; _set = context.Set<T>(); }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _set.FindAsync([id], ct).AsTask();

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await _set.AddAsync(entity, ct);

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
```

- [ ] **Step 3: Create stub entity repositories (full implementation in Task 7)**

`backend/src/CliniSys.Infrastructure/Persistence/Repositories/AppointmentRepository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(AppDbContext context) : base(context) { }
    public Task<List<Appointment>> GetByDoctorAndDateAsync(Guid doctorId, DateOnly date, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<PagedResult<Appointment>> GetPagedAsync(Guid? doctorId, Guid? patientId, DateOnly? date, DateTime? startDate, DateTime? endDate, AppointmentStatus? status, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
}
```

`backend/src/CliniSys.Infrastructure/Persistence/Repositories/PatientRepository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class PatientRepository : Repository<Patient>, IPatientRepository
{
    public PatientRepository(AppDbContext context) : base(context) { }
    public Task<PagedResult<Patient>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
}
```

`backend/src/CliniSys.Infrastructure/Persistence/Repositories/DoctorRepository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class DoctorRepository : Repository<Doctor>, IDoctorRepository
{
    public DoctorRepository(AppDbContext context) : base(context) { }
    public Task<PagedResult<Doctor>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Doctor?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
}
```

`backend/src/CliniSys.Infrastructure/Persistence/Repositories/UserRepository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class UserRepository : Repository<ApplicationUser>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }
    public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<PagedResult<ApplicationUser>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
}
```

`backend/src/CliniSys.Infrastructure/Persistence/Repositories/ClinicSettingsRepository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class ClinicSettingsRepository : Repository<ClinicSettings>, IClinicSettingsRepository
{
    public ClinicSettingsRepository(AppDbContext context) : base(context) { }
    public Task<ClinicSettings> GetSingletonAsync(CancellationToken ct = default) => throw new NotImplementedException();
}
```

- [ ] **Step 4: Create stub IdentityService**

`backend/src/CliniSys.Infrastructure/Identity/IdentityService.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Domain.Enums;

namespace CliniSys.Infrastructure.Identity;

internal class IdentityService : IIdentityService
{
    public Task<Guid> CreateUserAsync(string email, string fullName, string password, Role role, CancellationToken ct = default) => throw new NotImplementedException();
    public Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default) => throw new NotImplementedException();
    public Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default) => throw new NotImplementedException();
    public Task DeactivateUserAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
}
```

- [ ] **Step 5: Create Infrastructure DI extension**

`backend/src/CliniSys.Infrastructure/DependencyInjection.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;
using CliniSys.Infrastructure.Identity;
using CliniSys.Infrastructure.Persistence;
using CliniSys.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CliniSys.Infrastructure;

/// <summary>Registers all Infrastructure-layer services.</summary>
public static class DependencyInjection
{
    /// <summary>Adds EF Core, Identity, OpenIddict, and all repository implementations.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseOpenIddict<Guid>();
        });

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AppDbContext>())
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/connect/token");
                options.AllowPasswordFlow();
                options.AcceptAnonymousClients();
                options.UseAspNetCore().EnableTokenEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClinicSettingsRepository, ClinicSettingsRepository>();
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}
```

- [ ] **Step 6: Verify build**

```bash
cd backend && dotnet build
```
Expected: `Build succeeded. 0 Error(s)` (NotImplementedException stubs are fine)

- [ ] **Step 7: Commit**

```bash
git add backend/src/CliniSys.Infrastructure/
git commit -m "feat: add AppDbContext, Infrastructure DI, and repository/identity stubs"
```

---

### Task 7: Repository + IdentityService Implementations

**Files:**
- Modify: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/AppointmentRepository.cs`
- Modify: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/PatientRepository.cs`
- Modify: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/DoctorRepository.cs`
- Modify: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/UserRepository.cs`
- Modify: `backend/src/CliniSys.Infrastructure/Persistence/Repositories/ClinicSettingsRepository.cs`
- Modify: `backend/src/CliniSys.Infrastructure/Identity/IdentityService.cs`

**Interfaces:**
- Produces: working implementations of all 5 repositories and IdentityService

- [ ] **Step 1: Implement AppointmentRepository**

Replace entire content of `AppointmentRepository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(AppDbContext context) : base(context) { }

    public async Task<List<Appointment>> GetByDoctorAndDateAsync(
        Guid doctorId, DateOnly date, CancellationToken ct = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end   = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        return await _set
            .Where(a => a.DoctorId == doctorId
                     && a.Status != AppointmentStatus.Cancelled
                     && a.StartsAt >= start && a.StartsAt <= end)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Appointment>> GetPagedAsync(
        Guid? doctorId, Guid? patientId, DateOnly? date,
        DateTime? startDate, DateTime? endDate, AppointmentStatus? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _set
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .AsQueryable();

        if (doctorId.HasValue)  query = query.Where(a => a.DoctorId  == doctorId);
        if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId);
        if (status.HasValue)    query = query.Where(a => a.Status    == status);

        if (startDate.HasValue && endDate.HasValue)
        {
            query = query.Where(a => a.StartsAt >= startDate && a.StartsAt <= endDate);
            var all = await query.OrderBy(a => a.StartsAt).ToListAsync(ct);
            return new PagedResult<Appointment>(all, 1, all.Count, all.Count, 1);
        }

        if (date.HasValue)
        {
            var s = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var e = date.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(a => a.StartsAt >= s && a.StartsAt <= e);
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(a => a.StartsAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Appointment>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
```

- [ ] **Step 2: Implement PatientRepository**

Replace entire content of `PatientRepository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class PatientRepository : Repository<Patient>, IPatientRepository
{
    public PatientRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<Patient>> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _set.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.FullName, $"%{search}%"));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Patient>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
```

- [ ] **Step 3: Implement DoctorRepository**

Replace entire content of `DoctorRepository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class DoctorRepository : Repository<Doctor>, IDoctorRepository
{
    public DoctorRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<Doctor>> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _set.Include(d => d.User).Where(d => d.IsActive);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(d => d.User.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Doctor>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    public Task<Doctor?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _set.Include(d => d.User).FirstOrDefaultAsync(d => d.UserId == userId, ct);
}
```

- [ ] **Step 4: Implement UserRepository**

Replace entire content of `UserRepository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class UserRepository : Repository<ApplicationUser>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _set.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<PagedResult<ApplicationUser>> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var total = await _set.CountAsync(ct);
        var items = await _set.OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<ApplicationUser>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
```

- [ ] **Step 5: Implement ClinicSettingsRepository**

Replace entire content of `ClinicSettingsRepository.cs`:
```csharp
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class ClinicSettingsRepository : Repository<ClinicSettings>, IClinicSettingsRepository
{
    private static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public ClinicSettingsRepository(AppDbContext context) : base(context) { }

    public async Task<ClinicSettings> GetSingletonAsync(CancellationToken ct = default)
    {
        var s = await _set.FirstOrDefaultAsync(ct);
        if (s is not null) return s;

        s = new ClinicSettings
        {
            Id = SingletonId,
            OpenTime  = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(18, 0),
            OpenDays  = "1,2,3,4,5"
        };
        await _set.AddAsync(s, ct);
        await _context.SaveChangesAsync(ct);
        return s;
    }
}
```

- [ ] **Step 6: Implement IdentityService**

Replace entire content of `IdentityService.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CliniSys.Infrastructure.Identity;

internal class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager) =>
        _userManager = userManager;

    public async Task<Guid> CreateUserAsync(
        string email, string fullName, string password, Role role, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            Id       = Guid.NewGuid(),
            UserName = email,
            Email    = email,
            FullName = fullName,
            Role     = role
        };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task DeactivateUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
    }
}
```

- [ ] **Step 7: Verify build**

```bash
cd backend && dotnet build
```

- [ ] **Step 8: Commit**

```bash
git add backend/src/CliniSys.Infrastructure/
git commit -m "feat: implement repositories and IdentityService"
```

---

### Task 8: API Foundation

**Files:**
- Create: `backend/src/CliniSys.Api/Middleware/ExceptionMiddleware.cs`
- Create: `backend/src/CliniSys.Api/Program.cs`
- Create: `backend/src/CliniSys.Api/appsettings.json`
- Create: `backend/src/CliniSys.Api/appsettings.Development.json`

**Interfaces:**
- Produces: runnable API with global exception handling, Swagger (XML comments), CORS, OpenIddict validation middleware

- [ ] **Step 1: Create ExceptionMiddleware**

`backend/src/CliniSys.Api/Middleware/ExceptionMiddleware.cs`:
```csharp
using System.Net;
using System.Text.Json;
using CliniSys.Application.Common.Exceptions;
using FluentValidation;

namespace CliniSys.Api.Middleware;

/// <summary>
/// Global exception-handling middleware. Maps known exception types to consistent
/// JSON responses and logs unhandled errors server-side.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    /// <summary>Initialises the middleware.</summary>
    /// <param name="next">Next middleware in the pipeline.</param>
    /// <param name="logger">Logger for unhandled exceptions.</param>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    /// <summary>Catches exceptions and writes a JSON error response.</summary>
    /// <param name="context">HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (ValidationException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, "Validation failed.",
                ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (NotFoundException ex)  { await WriteAsync(context, HttpStatusCode.NotFound,    ex.Message); }
        catch (ConflictException ex)  { await WriteAsync(context, HttpStatusCode.Conflict,    ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");
            await WriteAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteAsync(
        HttpContext ctx, HttpStatusCode code, string message, IEnumerable<string>? errors = null)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = (int)code;
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var body = errors is not null ? (object)new { message, errors } : new { message };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body, opts));
    }
}
```

- [ ] **Step 2: Create Program.cs**

`backend/src/CliniSys.Api/Program.cs`:
```csharp
using System.Reflection;
using CliniSys.Api.Middleware;
using CliniSys.Application;
using CliniSys.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(xmlPath);
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization", Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {{
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
    }});
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

- [ ] **Step 3: Create appsettings files**

`backend/src/CliniSys.Api/appsettings.json`:
```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*"
}
```

`backend/src/CliniSys.Api/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=clinisys;Username=clinisys;Password=clinisys"
  },
  "Logging": { "LogLevel": { "Default": "Debug", "Microsoft.AspNetCore": "Information" } }
}
```

- [ ] **Step 4: Verify build**

```bash
cd backend && dotnet build
```

- [ ] **Step 5: Commit**

```bash
git add backend/src/CliniSys.Api/
git commit -m "feat: add API foundation — Program.cs, ExceptionMiddleware, Swagger, CORS"
```

---

### Task 9: OpenIddict Token Controller

**Files:**
- Create: `backend/src/CliniSys.Api/Controllers/ConnectController.cs`

**Interfaces:**
- Consumes: `IDoctorRepository`, `UserManager<ApplicationUser>` (injected directly — Identity SDK is acceptable in the Api layer)
- Produces: `POST /connect/token` — issues JWT with custom claims `role`, `theme`, `language`, `doctorId`

- [ ] **Step 1: Create ConnectController**

`backend/src/CliniSys.Api/Controllers/ConnectController.cs`:
```csharp
using System.Security.Claims;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CliniSys.Api.Controllers;

/// <summary>
/// Handles the OpenIddict ROPC token endpoint passthrough.
/// Issues JWT access tokens for valid username/password credentials.
/// </summary>
[ApiController]
public class ConnectController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDoctorRepository _doctorRepository;

    /// <summary>Initialises the controller.</summary>
    /// <param name="userManager">ASP.NET Core Identity user manager.</param>
    /// <param name="doctorRepository">Repository to resolve the caller's linked doctor profile.</param>
    public ConnectController(UserManager<ApplicationUser> userManager, IDoctorRepository doctorRepository)
    {
        _userManager      = userManager;
        _doctorRepository = doctorRepository;
    }

    /// <summary>
    /// Issues an OAuth 2.0 access token for a valid username/password pair.
    /// Custom claims in the token: <c>role</c>, <c>theme</c>, <c>language</c>, <c>fullName</c>,
    /// and <c>doctorId</c> (Doctor role only).
    /// </summary>
    /// <returns>Standard OAuth 2.0 token response.</returns>
    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict server request cannot be retrieved.");

        if (!request.IsPasswordGrantType())
            throw new InvalidOperationException("Grant type not supported.");

        var user = await _userManager.FindByNameAsync(request.Username!);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password!))
        {
            var props = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error]            = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid credentials."
            });
            return Forbid(props, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name, roleType: Claims.Role);

        identity
            .SetClaim(Claims.Subject, user.Id.ToString())
            .SetClaim(Claims.Name,    user.Email!)
            .SetClaim("role",         user.Role.ToString())
            .SetClaim("theme",        user.ThemePreference.ToString())
            .SetClaim("language",     user.LanguagePreference)
            .SetClaim("fullName",     user.FullName);

        if (user.Role == CliniSys.Domain.Enums.Role.Doctor)
        {
            var doctor = await _doctorRepository.GetByUserIdAsync(user.Id);
            if (doctor is not null)
                identity.SetClaim("doctorId", doctor.Id.ToString());
        }

        identity.SetDestinations(c => c.Type switch
        {
            Claims.Name or Claims.Email => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        });

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
```

- [ ] **Step 2: Verify build**

```bash
cd backend && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add backend/src/CliniSys.Api/Controllers/ConnectController.cs
git commit -m "feat: add OpenIddict ROPC token endpoint with custom JWT claims"
```

---

### Task 10: ClinicSettings Feature

**Files:**
- Create: `backend/src/CliniSys.Application/Queries/ClinicSettings/GetClinicSettings/GetClinicSettingsQuery.cs`
- Create: `backend/src/CliniSys.Application/Queries/ClinicSettings/GetClinicSettings/GetClinicSettingsQueryHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/ClinicSettings/UpdateClinicSettings/UpdateClinicSettingsCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/ClinicSettings/UpdateClinicSettings/UpdateClinicSettingsCommandValidator.cs`
- Create: `backend/src/CliniSys.Application/Commands/ClinicSettings/UpdateClinicSettings/UpdateClinicSettingsCommandHandler.cs`
- Create: `backend/src/CliniSys.Api/Requests/ClinicSettings/UpdateClinicSettingsRequest.cs`
- Create: `backend/src/CliniSys.Api/Controllers/ClinicSettingsController.cs`

**Interfaces:**
- Produces: `GET /api/clinic-settings` (all auth), `PUT /api/clinic-settings` (Admin)

- [ ] **Step 1: Create query + handler**

`backend/src/CliniSys.Application/Queries/ClinicSettings/GetClinicSettings/GetClinicSettingsQuery.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;

namespace CliniSys.Application.Queries.ClinicSettings.GetClinicSettings;

/// <summary>Query that retrieves the singleton clinic settings row.</summary>
public record GetClinicSettingsQuery : IQuery<ClinicSettingsModel>;
```

`backend/src/CliniSys.Application/Queries/ClinicSettings/GetClinicSettings/GetClinicSettingsQueryHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;

namespace CliniSys.Application.Queries.ClinicSettings.GetClinicSettings;

/// <summary>Clinic settings response model.</summary>
/// <param name="Id">Settings identifier.</param>
/// <param name="OpenTime">Opening time in HH:mm.</param>
/// <param name="CloseTime">Closing time in HH:mm.</param>
/// <param name="OpenDays">Comma-separated weekday numbers (0=Sun…6=Sat).</param>
/// <param name="LogoBase64">Base64 data URI or <see langword="null"/>.</param>
public record ClinicSettingsModel(Guid Id, string OpenTime, string CloseTime, string OpenDays, string? LogoBase64);

/// <summary>Handler for <see cref="GetClinicSettingsQuery"/>.</summary>
public class GetClinicSettingsQueryHandler : IQueryHandler<GetClinicSettingsQuery, ClinicSettingsModel>
{
    private readonly IClinicSettingsRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Clinic settings repository.</param>
    public GetClinicSettingsQueryHandler(IClinicSettingsRepository repo) => _repo = repo;

    /// <summary>Returns the current clinic settings.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Clinic settings model.</returns>
    public async Task<ClinicSettingsModel> Handle(GetClinicSettingsQuery request, CancellationToken cancellationToken)
    {
        var s = await _repo.GetSingletonAsync(cancellationToken);
        return new ClinicSettingsModel(s.Id, s.OpenTime.ToString("HH:mm"),
            s.CloseTime.ToString("HH:mm"), s.OpenDays, s.LogoBase64);
    }
}
```

- [ ] **Step 2: Create command + validator + handler**

`backend/src/CliniSys.Application/Commands/ClinicSettings/UpdateClinicSettings/UpdateClinicSettingsCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.ClinicSettings.UpdateClinicSettings;

/// <summary>Command to update clinic-wide settings.</summary>
/// <param name="OpenTime">Opening time in HH:mm.</param>
/// <param name="CloseTime">Closing time in HH:mm.</param>
/// <param name="OpenDays">Comma-separated weekday numbers (0=Sun…6=Sat).</param>
/// <param name="LogoBase64">Base64 data URI or <see langword="null"/> to clear the logo.</param>
public record UpdateClinicSettingsCommand(
    string OpenTime, string CloseTime, string OpenDays, string? LogoBase64) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/ClinicSettings/UpdateClinicSettings/UpdateClinicSettingsCommandValidator.cs`:
```csharp
using FluentValidation;

namespace CliniSys.Application.Commands.ClinicSettings.UpdateClinicSettings;

/// <summary>Validates <see cref="UpdateClinicSettingsCommand"/>.</summary>
public class UpdateClinicSettingsCommandValidator : AbstractValidator<UpdateClinicSettingsCommand>
{
    /// <summary>Defines validation rules.</summary>
    public UpdateClinicSettingsCommandValidator()
    {
        RuleFor(x => x.OpenTime).NotEmpty().Matches(@"^\d{2}:\d{2}$").WithMessage("OpenTime must be HH:mm.");
        RuleFor(x => x.CloseTime).NotEmpty().Matches(@"^\d{2}:\d{2}$").WithMessage("CloseTime must be HH:mm.");
        RuleFor(x => x.OpenDays).NotEmpty().Matches(@"^[0-6](,[0-6])*$").WithMessage("OpenDays must be comma-separated 0–6.");
        RuleFor(x => x.LogoBase64).Must(IsValidImage).When(x => x.LogoBase64 is not null)
            .WithMessage("LogoBase64 must be a valid base64 image data URI (max 512 KB).");
    }

    private static bool IsValidImage(string? v)
    {
        if (v is null || !v.StartsWith("data:image/")) return false;
        var i = v.IndexOf(',');
        return i >= 0 && (v[(i + 1)..].Length * 3 / 4) <= 512 * 1024;
    }
}
```

`backend/src/CliniSys.Application/Commands/ClinicSettings/UpdateClinicSettings/UpdateClinicSettingsCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.ClinicSettings.UpdateClinicSettings;

/// <summary>Handler for <see cref="UpdateClinicSettingsCommand"/>.</summary>
public class UpdateClinicSettingsCommandHandler : ICommandHandler<UpdateClinicSettingsCommand, Unit>
{
    private readonly IClinicSettingsRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Clinic settings repository.</param>
    public UpdateClinicSettingsCommandHandler(IClinicSettingsRepository repo) => _repo = repo;

    /// <summary>Updates the singleton clinic settings row.</summary>
    /// <param name="request">Update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdateClinicSettingsCommand request, CancellationToken cancellationToken)
    {
        var s = await _repo.GetSingletonAsync(cancellationToken);
        s.OpenTime   = TimeOnly.Parse(request.OpenTime);
        s.CloseTime  = TimeOnly.Parse(request.CloseTime);
        s.OpenDays   = request.OpenDays;
        s.LogoBase64 = request.LogoBase64;
        _repo.Update(s);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 3: Create request model and controller**

`backend/src/CliniSys.Api/Requests/ClinicSettings/UpdateClinicSettingsRequest.cs`:
```csharp
namespace CliniSys.Api.Requests.ClinicSettings;

/// <summary>HTTP body for PUT /api/clinic-settings.</summary>
/// <param name="OpenTime">Opening time HH:mm.</param>
/// <param name="CloseTime">Closing time HH:mm.</param>
/// <param name="OpenDays">Comma-separated weekday numbers.</param>
/// <param name="LogoBase64">Base64 data URI or <see langword="null"/> to remove logo.</param>
public record UpdateClinicSettingsRequest(string OpenTime, string CloseTime, string OpenDays, string? LogoBase64);
```

`backend/src/CliniSys.Api/Controllers/ClinicSettingsController.cs`:
```csharp
using CliniSys.Api.Requests.ClinicSettings;
using CliniSys.Application.Commands.ClinicSettings.UpdateClinicSettings;
using CliniSys.Application.Queries.ClinicSettings.GetClinicSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for reading and updating clinic-wide settings.</summary>
[ApiController, Route("api/clinic-settings"), Authorize]
public class ClinicSettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public ClinicSettingsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns current clinic settings. All authenticated roles.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Clinic settings.</returns>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetClinicSettingsQuery(), ct));

    /// <summary>Updates clinic settings. Admin only.</summary>
    /// <param name="request">New settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] UpdateClinicSettingsRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateClinicSettingsCommand(
            request.OpenTime, request.CloseTime, request.OpenDays, request.LogoBase64), ct);
        return NoContent();
    }
}
```

- [ ] **Step 4: Verify build and commit**

```bash
cd backend && dotnet build
git add backend/src/CliniSys.Application/Queries/ClinicSettings/ \
         backend/src/CliniSys.Application/Commands/ClinicSettings/ \
         backend/src/CliniSys.Api/Requests/ClinicSettings/ \
         backend/src/CliniSys.Api/Controllers/ClinicSettingsController.cs
git commit -m "feat: add ClinicSettings feature (GET/PUT)"
```

---

### Task 11: Patients Feature

**Files:**
- Create: `backend/src/CliniSys.Application/Queries/Patients/GetPatients/GetPatientsQuery.cs`
- Create: `backend/src/CliniSys.Application/Queries/Patients/GetPatients/GetPatientsQueryHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Patients/CreatePatient/CreatePatientCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Patients/CreatePatient/CreatePatientCommandValidator.cs`
- Create: `backend/src/CliniSys.Application/Commands/Patients/CreatePatient/CreatePatientCommandHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Patients/UpdatePatient/UpdatePatientCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Patients/UpdatePatient/UpdatePatientCommandValidator.cs`
- Create: `backend/src/CliniSys.Application/Commands/Patients/UpdatePatient/UpdatePatientCommandHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Patients/DeactivatePatient/DeactivatePatientCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Patients/DeactivatePatient/DeactivatePatientCommandHandler.cs`
- Create: `backend/src/CliniSys.Api/Requests/Patients/CreatePatientRequest.cs`
- Create: `backend/src/CliniSys.Api/Requests/Patients/UpdatePatientRequest.cs`
- Create: `backend/src/CliniSys.Api/Controllers/PatientsController.cs`

**Interfaces:**
- Produces: `GET /api/patients`, `GET /api/patients/{id}`, `POST`, `PUT /{id}`, `DELETE /{id}`

- [ ] **Step 1: Create query + handler**

`backend/src/CliniSys.Application/Queries/Patients/GetPatients/GetPatientsQuery.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Models;

namespace CliniSys.Application.Queries.Patients.GetPatients;

/// <summary>Query to retrieve a paginated, searchable list of active patients.</summary>
/// <param name="Search">Optional case-insensitive name filter.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 100).</param>
public record GetPatientsQuery(string? Search = null, int Page = 1, int PageSize = 20)
    : IPagedQuery<PatientModel>;
```

`backend/src/CliniSys.Application/Queries/Patients/GetPatients/GetPatientsQueryHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using FluentValidation;

namespace CliniSys.Application.Queries.Patients.GetPatients;

/// <summary>Patient response model.</summary>
/// <param name="Id">Patient identifier.</param>
/// <param name="FullName">Full name.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="Phone">Contact phone.</param>
/// <param name="Email">Optional email.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="IsActive">Active status.</param>
public record PatientModel(Guid Id, string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes, bool IsActive);

/// <summary>Handler for <see cref="GetPatientsQuery"/>.</summary>
public class GetPatientsQueryHandler : IQueryHandler<GetPatientsQuery, PagedResult<PatientModel>>
{
    private readonly IPatientRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Patient repository.</param>
    public GetPatientsQueryHandler(IPatientRepository repo) => _repo = repo;

    /// <summary>Returns a paginated filtered list of patients.</summary>
    /// <param name="request">Query with filters and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated patient list.</returns>
    public async Task<PagedResult<PatientModel>> Handle(
        GetPatientsQuery request, CancellationToken cancellationToken)
    {
        if (request.PageSize > 100) throw new ValidationException("PageSize cannot exceed 100.");

        var paged = await _repo.GetPagedAsync(request.Search, request.Page, request.PageSize, cancellationToken);
        var items = paged.Items.Select(p =>
            new PatientModel(p.Id, p.FullName, p.DateOfBirth, p.Phone, p.Email, p.Notes, p.IsActive)).ToList();
        return new PagedResult<PatientModel>(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }
}
```

- [ ] **Step 2: Create CreatePatient command + validator + handler**

`backend/src/CliniSys.Application/Commands/Patients/CreatePatient/CreatePatientCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;

namespace CliniSys.Application.Commands.Patients.CreatePatient;

/// <summary>Command to register a new patient.</summary>
/// <param name="FullName">Patient full name.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="Phone">Contact phone.</param>
/// <param name="Email">Optional email.</param>
/// <param name="Notes">Optional notes.</param>
public record CreatePatientCommand(string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes) : ICommand<Guid>;
```

`backend/src/CliniSys.Application/Commands/Patients/CreatePatient/CreatePatientCommandValidator.cs`:
```csharp
using FluentValidation;

namespace CliniSys.Application.Commands.Patients.CreatePatient;

/// <summary>Validates <see cref="CreatePatientCommand"/>.</summary>
public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    /// <summary>Defines validation rules.</summary>
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateOfBirth).NotEmpty().LessThan(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
    }
}
```

`backend/src/CliniSys.Application/Commands/Patients/CreatePatient/CreatePatientCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;

namespace CliniSys.Application.Commands.Patients.CreatePatient;

/// <summary>Handler for <see cref="CreatePatientCommand"/>.</summary>
public class CreatePatientCommandHandler : ICommandHandler<CreatePatientCommand, Guid>
{
    private readonly IPatientRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Patient repository.</param>
    public CreatePatientCommandHandler(IPatientRepository repo) => _repo = repo;

    /// <summary>Creates a new patient record and returns its ID.</summary>
    /// <param name="request">Patient creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new patient's <see cref="Guid"/>.</returns>
    public async Task<Guid> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(), FullName = request.FullName,
            DateOfBirth = request.DateOfBirth, Phone = request.Phone,
            Email = request.Email, Notes = request.Notes
        };
        await _repo.AddAsync(patient, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);
        return patient.Id;
    }
}
```

- [ ] **Step 3: Create UpdatePatient command + validator + handler**

`backend/src/CliniSys.Application/Commands/Patients/UpdatePatient/UpdatePatientCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Patients.UpdatePatient;

/// <summary>Command to update an existing patient.</summary>
/// <param name="Id">Patient identifier.</param>
/// <param name="FullName">Updated full name.</param>
/// <param name="DateOfBirth">Updated date of birth.</param>
/// <param name="Phone">Updated phone.</param>
/// <param name="Email">Updated optional email.</param>
/// <param name="Notes">Updated optional notes.</param>
public record UpdatePatientCommand(Guid Id, string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Patients/UpdatePatient/UpdatePatientCommandValidator.cs`:
```csharp
using FluentValidation;

namespace CliniSys.Application.Commands.Patients.UpdatePatient;

/// <summary>Validates <see cref="UpdatePatientCommand"/>.</summary>
public class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    /// <summary>Defines validation rules.</summary>
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateOfBirth).NotEmpty().LessThan(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
    }
}
```

`backend/src/CliniSys.Application/Commands/Patients/UpdatePatient/UpdatePatientCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.Patients.UpdatePatient;

/// <summary>Handler for <see cref="UpdatePatientCommand"/>.</summary>
public class UpdatePatientCommandHandler : ICommandHandler<UpdatePatientCommand, Unit>
{
    private readonly IPatientRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Patient repository.</param>
    public UpdatePatientCommandHandler(IPatientRepository repo) => _repo = repo;

    /// <summary>Updates the patient's details.</summary>
    /// <param name="request">Update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Patient {request.Id} not found.");
        patient.FullName    = request.FullName;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Phone       = request.Phone;
        patient.Email       = request.Email;
        patient.Notes       = request.Notes;
        _repo.Update(patient);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 4: Create DeactivatePatient command + handler**

`backend/src/CliniSys.Application/Commands/Patients/DeactivatePatient/DeactivatePatientCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Patients.DeactivatePatient;

/// <summary>Command to soft-delete a patient (sets IsActive = false).</summary>
/// <param name="Id">Patient identifier.</param>
public record DeactivatePatientCommand(Guid Id) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Patients/DeactivatePatient/DeactivatePatientCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.Patients.DeactivatePatient;

/// <summary>Handler for <see cref="DeactivatePatientCommand"/>.</summary>
public class DeactivatePatientCommandHandler : ICommandHandler<DeactivatePatientCommand, Unit>
{
    private readonly IPatientRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Patient repository.</param>
    public DeactivatePatientCommandHandler(IPatientRepository repo) => _repo = repo;

    /// <summary>Sets the patient's IsActive flag to false.</summary>
    /// <param name="request">Deactivation command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(DeactivatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Patient {request.Id} not found.");
        patient.IsActive = false;
        _repo.Update(patient);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 5: Create request models and controller**

`backend/src/CliniSys.Api/Requests/Patients/CreatePatientRequest.cs`:
```csharp
namespace CliniSys.Api.Requests.Patients;

/// <summary>HTTP body for POST /api/patients.</summary>
/// <param name="FullName">Patient full name.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="Phone">Contact phone.</param>
/// <param name="Email">Optional email.</param>
/// <param name="Notes">Optional notes.</param>
public record CreatePatientRequest(string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes);
```

`backend/src/CliniSys.Api/Requests/Patients/UpdatePatientRequest.cs`:
```csharp
namespace CliniSys.Api.Requests.Patients;

/// <summary>HTTP body for PUT /api/patients/{id}.</summary>
/// <param name="FullName">Updated full name.</param>
/// <param name="DateOfBirth">Updated date of birth.</param>
/// <param name="Phone">Updated phone.</param>
/// <param name="Email">Updated optional email.</param>
/// <param name="Notes">Updated optional notes.</param>
public record UpdatePatientRequest(string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes);
```

`backend/src/CliniSys.Api/Controllers/PatientsController.cs`:
```csharp
using CliniSys.Api.Requests.Patients;
using CliniSys.Application.Commands.Patients.CreatePatient;
using CliniSys.Application.Commands.Patients.DeactivatePatient;
using CliniSys.Application.Commands.Patients.UpdatePatient;
using CliniSys.Application.Queries.Patients.GetPatients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for managing patient records.</summary>
[ApiController, Route("api/patients"), Authorize(Roles = "Admin,Staff")]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public PatientsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns a paginated list of active patients, optionally filtered by name.</summary>
    /// <param name="search">Optional name filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged patient list.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetPatientsQuery(search, page, pageSize), ct));

    /// <summary>Returns a single patient by ID.</summary>
    /// <param name="id">Patient identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The patient or 404.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPatientsQuery(null, 1, 1), ct);
        var patient = result.Items.FirstOrDefault(p => p.Id == id);
        return patient is null ? NotFound() : Ok(patient);
    }

    /// <summary>Creates a new patient.</summary>
    /// <param name="request">Patient creation data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with the new patient ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreatePatientCommand(
            request.FullName, request.DateOfBirth, request.Phone, request.Email, request.Notes), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Updates a patient's details.</summary>
    /// <param name="id">Patient identifier.</param>
    /// <param name="request">Updated data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdatePatientRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdatePatientCommand(
            id, request.FullName, request.DateOfBirth, request.Phone, request.Email, request.Notes), ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a patient (sets IsActive = false).</summary>
    /// <param name="id">Patient identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivatePatientCommand(id), ct);
        return NoContent();
    }
}
```

- [ ] **Step 6: Verify build and commit**

```bash
cd backend && dotnet build
git add backend/src/CliniSys.Application/Queries/Patients/ \
         backend/src/CliniSys.Application/Commands/Patients/ \
         backend/src/CliniSys.Api/Requests/Patients/ \
         backend/src/CliniSys.Api/Controllers/PatientsController.cs
git commit -m "feat: add Patients feature (CRUD + soft delete)"
```

---

### Task 12: Doctors Feature

**Files:**
- Create: `backend/src/CliniSys.Application/Queries/Doctors/GetDoctors/GetDoctorsQuery.cs`
- Create: `backend/src/CliniSys.Application/Queries/Doctors/GetDoctors/GetDoctorsQueryHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Doctors/UpdateDoctor/UpdateDoctorCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Doctors/UpdateDoctor/UpdateDoctorCommandHandler.cs`
- Create: `backend/src/CliniSys.Api/Requests/Doctors/UpdateDoctorRequest.cs`
- Create: `backend/src/CliniSys.Api/Controllers/DoctorsController.cs`

**Interfaces:**
- Produces: `GET /api/doctors`, `GET /api/doctors/{id}`, `PATCH /api/doctors/{id}` (Admin)

- [ ] **Step 1: Create query + handler**

`backend/src/CliniSys.Application/Queries/Doctors/GetDoctors/GetDoctorsQuery.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Models;

namespace CliniSys.Application.Queries.Doctors.GetDoctors;

/// <summary>Query to retrieve a paginated list of active doctors.</summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 100).</param>
public record GetDoctorsQuery(int Page = 1, int PageSize = 20) : IPagedQuery<DoctorModel>;
```

`backend/src/CliniSys.Application/Queries/Doctors/GetDoctors/GetDoctorsQueryHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using FluentValidation;

namespace CliniSys.Application.Queries.Doctors.GetDoctors;

/// <summary>Doctor response model.</summary>
/// <param name="Id">Doctor identifier.</param>
/// <param name="UserId">Linked user identifier.</param>
/// <param name="FullName">Doctor's full name.</param>
/// <param name="Email">Doctor's email.</param>
/// <param name="Specialty">Medical specialty.</param>
/// <param name="IsActive">Active status.</param>
public record DoctorModel(Guid Id, Guid UserId, string FullName, string? Email, string Specialty, bool IsActive);

/// <summary>Handler for <see cref="GetDoctorsQuery"/>.</summary>
public class GetDoctorsQueryHandler : IQueryHandler<GetDoctorsQuery, PagedResult<DoctorModel>>
{
    private readonly IDoctorRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Doctor repository.</param>
    public GetDoctorsQueryHandler(IDoctorRepository repo) => _repo = repo;

    /// <summary>Returns paginated active doctors.</summary>
    /// <param name="request">Query with pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated doctor list.</returns>
    public async Task<PagedResult<DoctorModel>> Handle(
        GetDoctorsQuery request, CancellationToken cancellationToken)
    {
        if (request.PageSize > 100) throw new ValidationException("PageSize cannot exceed 100.");
        var paged = await _repo.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
        var items = paged.Items.Select(d =>
            new DoctorModel(d.Id, d.UserId, d.User.FullName, d.User.Email, d.Specialty, d.IsActive)).ToList();
        return new PagedResult<DoctorModel>(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }
}
```

- [ ] **Step 2: Create UpdateDoctor command + handler**

`backend/src/CliniSys.Application/Commands/Doctors/UpdateDoctor/UpdateDoctorCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Doctors.UpdateDoctor;

/// <summary>Command to update a doctor's specialty. Admin only.</summary>
/// <param name="Id">Doctor identifier.</param>
/// <param name="Specialty">Updated specialty.</param>
public record UpdateDoctorCommand(Guid Id, string Specialty) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Doctors/UpdateDoctor/UpdateDoctorCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.Doctors.UpdateDoctor;

/// <summary>Handler for <see cref="UpdateDoctorCommand"/>.</summary>
public class UpdateDoctorCommandHandler : ICommandHandler<UpdateDoctorCommand, Unit>
{
    private readonly IDoctorRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Doctor repository.</param>
    public UpdateDoctorCommandHandler(IDoctorRepository repo) => _repo = repo;

    /// <summary>Updates the doctor's specialty.</summary>
    /// <param name="request">Update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdateDoctorCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Doctor {request.Id} not found.");
        doctor.Specialty = request.Specialty;
        _repo.Update(doctor);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 3: Create request model and controller**

`backend/src/CliniSys.Api/Requests/Doctors/UpdateDoctorRequest.cs`:
```csharp
namespace CliniSys.Api.Requests.Doctors;

/// <summary>HTTP body for PATCH /api/doctors/{id}.</summary>
/// <param name="Specialty">Updated medical specialty.</param>
public record UpdateDoctorRequest(string Specialty);
```

`backend/src/CliniSys.Api/Controllers/DoctorsController.cs`:
```csharp
using CliniSys.Api.Requests.Doctors;
using CliniSys.Application.Commands.Doctors.UpdateDoctor;
using CliniSys.Application.Queries.Doctors.GetDoctors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for viewing and updating doctor profiles.</summary>
[ApiController, Route("api/doctors"), Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public DoctorsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns a paginated list of active doctors.</summary>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated doctor list.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetDoctorsQuery(page, pageSize), ct));

    /// <summary>Returns a single doctor by ID.</summary>
    /// <param name="id">Doctor identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The doctor or 404.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDoctorsQuery(1, 1), ct);
        var doctor = result.Items.FirstOrDefault(d => d.Id == id);
        return doctor is null ? NotFound() : Ok(doctor);
    }

    /// <summary>Updates a doctor's specialty. Admin only.</summary>
    /// <param name="id">Doctor identifier.</param>
    /// <param name="request">Updated specialty.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdateDoctorRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateDoctorCommand(id, request.Specialty), ct);
        return NoContent();
    }
}
```

- [ ] **Step 4: Verify build and commit**

```bash
cd backend && dotnet build
git add backend/src/CliniSys.Application/Queries/Doctors/ \
         backend/src/CliniSys.Application/Commands/Doctors/ \
         backend/src/CliniSys.Api/Requests/Doctors/ \
         backend/src/CliniSys.Api/Controllers/DoctorsController.cs
git commit -m "feat: add Doctors feature (GET list/single, PATCH specialty)"
```

---

### Task 13: Users Feature

**Files:**
- Create: `backend/src/CliniSys.Application/Queries/Users/GetUsers/GetUsersQuery.cs`
- Create: `backend/src/CliniSys.Application/Queries/Users/GetUsers/GetUsersQueryHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Users/CreateUser/CreateUserCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Users/CreateUser/CreateUserCommandValidator.cs`
- Create: `backend/src/CliniSys.Application/Commands/Users/CreateUser/CreateUserCommandHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Users/DeactivateUser/DeactivateUserCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Users/DeactivateUser/DeactivateUserCommandHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Users/ResetPassword/ResetPasswordCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Users/ResetPassword/ResetPasswordCommandValidator.cs`
- Create: `backend/src/CliniSys.Application/Commands/Users/ResetPassword/ResetPasswordCommandHandler.cs`
- Create: `backend/src/CliniSys.Api/Requests/Users/CreateUserRequest.cs`
- Create: `backend/src/CliniSys.Api/Requests/Users/ResetPasswordRequest.cs`
- Create: `backend/src/CliniSys.Api/Controllers/UsersController.cs`

**Interfaces:**
- Produces: `GET /api/users`, `POST /api/users`, `PATCH /api/users/{id}/deactivate`, `POST /api/users/{id}/reset-password` (Admin only)

- [ ] **Step 1: Create query + handler**

`backend/src/CliniSys.Application/Queries/Users/GetUsers/GetUsersQuery.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Models;

namespace CliniSys.Application.Queries.Users.GetUsers;

/// <summary>Query to retrieve a paginated list of all users.</summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 100).</param>
public record GetUsersQuery(int Page = 1, int PageSize = 20) : IPagedQuery<UserModel>;
```

`backend/src/CliniSys.Application/Queries/Users/GetUsers/GetUsersQueryHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Enums;
using FluentValidation;

namespace CliniSys.Application.Queries.Users.GetUsers;

/// <summary>User response model.</summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">Email address.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Role">User role.</param>
/// <param name="ThemePreference">Preferred theme.</param>
/// <param name="LanguagePreference">Preferred language.</param>
public record UserModel(Guid Id, string? Email, string FullName, Role Role,
    ThemePreference ThemePreference, string LanguagePreference);

/// <summary>Handler for <see cref="GetUsersQuery"/>.</summary>
public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PagedResult<UserModel>>
{
    private readonly IUserRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">User repository.</param>
    public GetUsersQueryHandler(IUserRepository repo) => _repo = repo;

    /// <summary>Returns paginated users.</summary>
    /// <param name="request">Query with pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated user list.</returns>
    public async Task<PagedResult<UserModel>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        if (request.PageSize > 100) throw new ValidationException("PageSize cannot exceed 100.");
        var paged = await _repo.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
        var items = paged.Items.Select(u =>
            new UserModel(u.Id, u.Email, u.FullName, u.Role, u.ThemePreference, u.LanguagePreference)).ToList();
        return new PagedResult<UserModel>(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }
}
```

- [ ] **Step 2: Create CreateUser command + validator + handler**

`backend/src/CliniSys.Application/Commands/Users/CreateUser/CreateUserCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Commands.Users.CreateUser;

/// <summary>Command to create a new user account. If Role is Doctor, also creates the Doctor profile.</summary>
/// <param name="Email">Email address (used as login username).</param>
/// <param name="FullName">Display name.</param>
/// <param name="Password">Initial plain-text password.</param>
/// <param name="Role">User role.</param>
/// <param name="Specialty">Required when Role is Doctor; ignored otherwise.</param>
public record CreateUserCommand(string Email, string FullName, string Password,
    Role Role, string? Specialty) : ICommand<Guid>;
```

`backend/src/CliniSys.Application/Commands/Users/CreateUser/CreateUserCommandValidator.cs`:
```csharp
using CliniSys.Domain.Enums;
using FluentValidation;

namespace CliniSys.Application.Commands.Users.CreateUser;

/// <summary>Validates <see cref="CreateUserCommand"/>.</summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>Defines validation rules.</summary>
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Specialty).NotEmpty().When(x => x.Role == Role.Doctor)
            .WithMessage("Specialty is required when role is Doctor.");
    }
}
```

`backend/src/CliniSys.Application/Commands/Users/CreateUser/CreateUserCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Commands.Users.CreateUser;

/// <summary>Handler for <see cref="CreateUserCommand"/>.</summary>
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IIdentityService _identity;
    private readonly IDoctorRepository _doctors;

    /// <summary>Initialises the handler.</summary>
    /// <param name="identity">Identity service for user creation.</param>
    /// <param name="doctors">Doctor repository for linked profile creation.</param>
    public CreateUserCommandHandler(IIdentityService identity, IDoctorRepository doctors)
    {
        _identity = identity; _doctors = doctors;
    }

    /// <summary>Creates the user account and, if role is Doctor, a linked Doctor profile.</summary>
    /// <param name="request">User creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new user's <see cref="Guid"/>.</returns>
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userId = await _identity.CreateUserAsync(
            request.Email, request.FullName, request.Password, request.Role, cancellationToken);

        if (request.Role == Role.Doctor)
        {
            var doctor = new Doctor { Id = Guid.NewGuid(), UserId = userId, Specialty = request.Specialty! };
            await _doctors.AddAsync(doctor, cancellationToken);
            await _doctors.SaveChangesAsync(cancellationToken);
        }

        return userId;
    }
}
```

- [ ] **Step 3: Create DeactivateUser and ResetPassword commands**

`backend/src/CliniSys.Application/Commands/Users/DeactivateUser/DeactivateUserCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.DeactivateUser;

/// <summary>Command to lock a user account indefinitely.</summary>
/// <param name="Id">User identifier.</param>
public record DeactivateUserCommand(Guid Id) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Users/DeactivateUser/DeactivateUserCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.DeactivateUser;

/// <summary>Handler for <see cref="DeactivateUserCommand"/>.</summary>
public class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand, Unit>
{
    private readonly IIdentityService _identity;
    /// <summary>Initialises the handler.</summary>
    /// <param name="identity">Identity service.</param>
    public DeactivateUserCommandHandler(IIdentityService identity) => _identity = identity;

    /// <summary>Locks out the user indefinitely.</summary>
    /// <param name="request">Deactivation command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        await _identity.DeactivateUserAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
```

`backend/src/CliniSys.Application/Commands/Users/ResetPassword/ResetPasswordCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.ResetPassword;

/// <summary>Command for Admin to reset any user's password without knowing the current one.</summary>
/// <param name="UserId">Target user identifier.</param>
/// <param name="NewPassword">New plain-text password.</param>
public record ResetPasswordCommand(Guid UserId, string NewPassword) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Users/ResetPassword/ResetPasswordCommandValidator.cs`:
```csharp
using FluentValidation;

namespace CliniSys.Application.Commands.Users.ResetPassword;

/// <summary>Validates <see cref="ResetPasswordCommand"/>.</summary>
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    /// <summary>Defines validation rules.</summary>
    public ResetPasswordCommandValidator() =>
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
}
```

`backend/src/CliniSys.Application/Commands/Users/ResetPassword/ResetPasswordCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.ResetPassword;

/// <summary>Handler for <see cref="ResetPasswordCommand"/>.</summary>
public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, Unit>
{
    private readonly IIdentityService _identity;
    /// <summary>Initialises the handler.</summary>
    /// <param name="identity">Identity service.</param>
    public ResetPasswordCommandHandler(IIdentityService identity) => _identity = identity;

    /// <summary>Resets the target user's password.</summary>
    /// <param name="request">Reset command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        await _identity.ResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 4: Create request models and controller**

`backend/src/CliniSys.Api/Requests/Users/CreateUserRequest.cs`:
```csharp
using CliniSys.Domain.Enums;

namespace CliniSys.Api.Requests.Users;

/// <summary>HTTP body for POST /api/users.</summary>
/// <param name="Email">Email address.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Password">Initial password.</param>
/// <param name="Role">User role.</param>
/// <param name="Specialty">Required when Role is Doctor.</param>
public record CreateUserRequest(string Email, string FullName, string Password, Role Role, string? Specialty);
```

`backend/src/CliniSys.Api/Requests/Users/ResetPasswordRequest.cs`:
```csharp
namespace CliniSys.Api.Requests.Users;

/// <summary>HTTP body for POST /api/users/{id}/reset-password.</summary>
/// <param name="NewPassword">The new password to set.</param>
public record ResetPasswordRequest(string NewPassword);
```

`backend/src/CliniSys.Api/Controllers/UsersController.cs`:
```csharp
using CliniSys.Api.Requests.Users;
using CliniSys.Application.Commands.Users.CreateUser;
using CliniSys.Application.Commands.Users.DeactivateUser;
using CliniSys.Application.Commands.Users.ResetPassword;
using CliniSys.Application.Queries.Users.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for Admin user management.</summary>
[ApiController, Route("api/users"), Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public UsersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns a paginated list of all users.</summary>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated user list.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetUsersQuery(page, pageSize), ct));

    /// <summary>Creates a new user account (and Doctor profile when role is Doctor).</summary>
    /// <param name="request">User creation data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with the new user ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateUserCommand(
            request.Email, request.FullName, request.Password, request.Role, request.Specialty), ct);
        return CreatedAtAction(null, new { id }, new { id });
    }

    /// <summary>Locks a user account indefinitely.</summary>
    /// <param name="id">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateUserCommand(id), ct);
        return NoContent();
    }

    /// <summary>Resets a user's password (Admin action; no current password required).</summary>
    /// <param name="id">Target user identifier.</param>
    /// <param name="request">New password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromRoute] Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _mediator.Send(new ResetPasswordCommand(id, request.NewPassword), ct);
        return NoContent();
    }
}
```

- [ ] **Step 5: Verify build and commit**

```bash
cd backend && dotnet build
git add backend/src/CliniSys.Application/Queries/Users/ \
         backend/src/CliniSys.Application/Commands/Users/ \
         backend/src/CliniSys.Api/Requests/Users/ \
         backend/src/CliniSys.Api/Controllers/UsersController.cs
git commit -m "feat: add Users feature (list, create with Doctor profile, deactivate, reset-password)"
```

---

### Task 14: Appointments Feature

**Files:**
- Create: `backend/src/CliniSys.Application/Queries/Appointments/GetAppointments/GetAppointmentsQuery.cs`
- Create: `backend/src/CliniSys.Application/Queries/Appointments/GetAppointments/GetAppointmentsQueryHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Appointments/CreateAppointment/CreateAppointmentCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Appointments/CreateAppointment/CreateAppointmentCommandValidator.cs`
- Create: `backend/src/CliniSys.Application/Commands/Appointments/CreateAppointment/CreateAppointmentCommandHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Appointments/RescheduleAppointment/RescheduleAppointmentCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Appointments/RescheduleAppointment/RescheduleAppointmentCommandValidator.cs`
- Create: `backend/src/CliniSys.Application/Commands/Appointments/RescheduleAppointment/RescheduleAppointmentCommandHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Appointments/UpdateAppointmentStatus/UpdateAppointmentStatusCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Appointments/UpdateAppointmentStatus/UpdateAppointmentStatusCommandHandler.cs`
- Create: `backend/src/CliniSys.Api/Requests/Appointments/CreateAppointmentRequest.cs`
- Create: `backend/src/CliniSys.Api/Requests/Appointments/RescheduleAppointmentRequest.cs`
- Create: `backend/src/CliniSys.Api/Requests/Appointments/UpdateAppointmentStatusRequest.cs`
- Create: `backend/src/CliniSys.Api/Controllers/AppointmentsController.cs`

**Interfaces:**
- Produces: `GET /api/appointments`, `POST`, `PUT /{id}`, `PATCH /{id}/status`

- [ ] **Step 1: Create GetAppointments query + handler**

`backend/src/CliniSys.Application/Queries/Appointments/GetAppointments/GetAppointmentsQuery.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Queries.Appointments.GetAppointments;

/// <summary>
/// Query for appointments. Supports list view (paginated) and calendar view (date range, no pagination).
/// When <see cref="StartDate"/> and <see cref="EndDate"/> are both provided, pagination is ignored.
/// </summary>
/// <param name="DoctorId">Optional doctor filter.</param>
/// <param name="PatientId">Optional patient filter.</param>
/// <param name="Date">Optional single-day filter.</param>
/// <param name="StartDate">Calendar range start (UTC).</param>
/// <param name="EndDate">Calendar range end (UTC).</param>
/// <param name="Status">Optional status filter.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 100).</param>
public record GetAppointmentsQuery(
    Guid? DoctorId = null, Guid? PatientId = null, DateOnly? Date = null,
    DateTime? StartDate = null, DateTime? EndDate = null,
    AppointmentStatus? Status = null,
    int Page = 1, int PageSize = 20) : IPagedQuery<AppointmentModel>;
```

`backend/src/CliniSys.Application/Queries/Appointments/GetAppointments/GetAppointmentsQueryHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Enums;
using FluentValidation;

namespace CliniSys.Application.Queries.Appointments.GetAppointments;

/// <summary>Appointment response model.</summary>
/// <param name="Id">Appointment identifier.</param>
/// <param name="PatientId">Patient identifier.</param>
/// <param name="PatientName">Patient full name.</param>
/// <param name="DoctorId">Doctor identifier.</param>
/// <param name="DoctorName">Doctor full name.</param>
/// <param name="StartsAt">UTC start time.</param>
/// <param name="DurationMinutes">Duration in minutes.</param>
/// <param name="Status">Current status.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
public record AppointmentModel(
    Guid Id, Guid PatientId, string PatientName,
    Guid DoctorId, string DoctorName,
    DateTime StartsAt, int DurationMinutes,
    AppointmentStatus Status, string? Notes, DateTime CreatedAt);

/// <summary>Handler for <see cref="GetAppointmentsQuery"/>.</summary>
public class GetAppointmentsQueryHandler : IQueryHandler<GetAppointmentsQuery, PagedResult<AppointmentModel>>
{
    private readonly IAppointmentRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Appointment repository.</param>
    public GetAppointmentsQueryHandler(IAppointmentRepository repo) => _repo = repo;

    /// <summary>Returns paginated or date-range appointments.</summary>
    /// <param name="request">Query filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated appointment list.</returns>
    public async Task<PagedResult<AppointmentModel>> Handle(
        GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        if (request.PageSize > 100) throw new ValidationException("PageSize cannot exceed 100.");

        var paged = await _repo.GetPagedAsync(
            request.DoctorId, request.PatientId, request.Date,
            request.StartDate, request.EndDate, request.Status,
            request.Page, request.PageSize, cancellationToken);

        var items = paged.Items.Select(a => new AppointmentModel(
            a.Id, a.PatientId, a.Patient.FullName,
            a.DoctorId, a.Doctor.User.FullName,
            a.StartsAt, a.DurationMinutes, a.Status, a.Notes, a.CreatedAt)).ToList();

        return new PagedResult<AppointmentModel>(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }
}
```

- [ ] **Step 2: Create CreateAppointment command + validator + handler**

`backend/src/CliniSys.Application/Commands/Appointments/CreateAppointment/CreateAppointmentCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;

namespace CliniSys.Application.Commands.Appointments.CreateAppointment;

/// <summary>Command to schedule a new appointment.</summary>
/// <param name="PatientId">Patient identifier.</param>
/// <param name="DoctorId">Doctor identifier.</param>
/// <param name="StartsAt">UTC start time.</param>
/// <param name="DurationMinutes">Duration in minutes.</param>
/// <param name="Notes">Optional notes.</param>
public record CreateAppointmentCommand(
    Guid PatientId, Guid DoctorId, DateTime StartsAt,
    int DurationMinutes, string? Notes) : ICommand<Guid>;
```

`backend/src/CliniSys.Application/Commands/Appointments/CreateAppointment/CreateAppointmentCommandValidator.cs`:
```csharp
using FluentValidation;

namespace CliniSys.Application.Commands.Appointments.CreateAppointment;

/// <summary>Validates <see cref="CreateAppointmentCommand"/>.</summary>
public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    /// <summary>Defines validation rules.</summary>
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.StartsAt).GreaterThan(DateTime.UtcNow).WithMessage("StartsAt must be in the future.");
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
    }
}
```

`backend/src/CliniSys.Application/Commands/Appointments/CreateAppointment/CreateAppointmentCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Commands.Appointments.CreateAppointment;

/// <summary>Handler for <see cref="CreateAppointmentCommand"/>.</summary>
public class CreateAppointmentCommandHandler : ICommandHandler<CreateAppointmentCommand, Guid>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IClinicSettingsRepository _settings;

    /// <summary>Initialises the handler.</summary>
    /// <param name="appointments">Appointment repository.</param>
    /// <param name="settings">Clinic settings repository for open hours validation.</param>
    public CreateAppointmentCommandHandler(
        IAppointmentRepository appointments, IClinicSettingsRepository settings)
    {
        _appointments = appointments; _settings = settings;
    }

    /// <summary>Validates open hours and overlap, then creates the appointment.</summary>
    /// <param name="request">Appointment data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new appointment's <see cref="Guid"/>.</returns>
    public async Task<Guid> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var clinic = await _settings.GetSingletonAsync(cancellationToken);
        ValidateOpenHours(request.StartsAt, request.DurationMinutes, clinic);

        var date = DateOnly.FromDateTime(request.StartsAt);
        var existing = await _appointments.GetByDoctorAndDateAsync(request.DoctorId, date, cancellationToken);
        CheckOverlap(request.StartsAt, request.DurationMinutes, existing, excludeId: null);

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(), PatientId = request.PatientId, DoctorId = request.DoctorId,
            StartsAt = request.StartsAt, DurationMinutes = request.DurationMinutes, Notes = request.Notes
        };
        await _appointments.AddAsync(appointment, cancellationToken);
        await _appointments.SaveChangesAsync(cancellationToken);
        return appointment.Id;
    }

    private static void ValidateOpenHours(DateTime startsAt, int durationMinutes, Domain.Entities.ClinicSettings clinic)
    {
        var day = (int)startsAt.DayOfWeek;
        var openDays = clinic.OpenDays.Split(',').Select(int.Parse).ToHashSet();
        if (!openDays.Contains(day))
            throw new ConflictException("The clinic is not open on that day.");

        var startTime = TimeOnly.FromDateTime(startsAt);
        var endTime   = startTime.AddMinutes(durationMinutes);
        if (startTime < clinic.OpenTime || endTime > clinic.CloseTime)
            throw new ConflictException("The appointment falls outside clinic open hours.");
    }

    private static void CheckOverlap(DateTime startsAt, int durationMinutes,
        List<Appointment> existing, Guid? excludeId)
    {
        var endsAt = startsAt.AddMinutes(durationMinutes);
        var conflict = existing.FirstOrDefault(a =>
            a.Id != excludeId &&
            a.Status != AppointmentStatus.Cancelled &&
            startsAt < a.StartsAt.AddMinutes(a.DurationMinutes) &&
            endsAt   > a.StartsAt);
        if (conflict is not null)
            throw new ConflictException("The doctor already has an appointment at that time.");
    }
}
```

- [ ] **Step 3: Create RescheduleAppointment command + validator + handler**

`backend/src/CliniSys.Application/Commands/Appointments/RescheduleAppointment/RescheduleAppointmentCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Appointments.RescheduleAppointment;

/// <summary>Command to reschedule an existing appointment to a new time.</summary>
/// <param name="Id">Appointment identifier.</param>
/// <param name="StartsAt">New UTC start time.</param>
/// <param name="DurationMinutes">New duration in minutes.</param>
public record RescheduleAppointmentCommand(Guid Id, DateTime StartsAt, int DurationMinutes) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Appointments/RescheduleAppointment/RescheduleAppointmentCommandValidator.cs`:
```csharp
using FluentValidation;

namespace CliniSys.Application.Commands.Appointments.RescheduleAppointment;

/// <summary>Validates <see cref="RescheduleAppointmentCommand"/>.</summary>
public class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    /// <summary>Defines validation rules.</summary>
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.StartsAt).GreaterThan(DateTime.UtcNow).WithMessage("StartsAt must be in the future.");
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
    }
}
```

`backend/src/CliniSys.Application/Commands/Appointments/RescheduleAppointment/RescheduleAppointmentCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Enums;
using MediatR;

namespace CliniSys.Application.Commands.Appointments.RescheduleAppointment;

/// <summary>Handler for <see cref="RescheduleAppointmentCommand"/>.</summary>
public class RescheduleAppointmentCommandHandler : ICommandHandler<RescheduleAppointmentCommand, Unit>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IClinicSettingsRepository _settings;

    /// <summary>Initialises the handler.</summary>
    /// <param name="appointments">Appointment repository.</param>
    /// <param name="settings">Clinic settings repository.</param>
    public RescheduleAppointmentCommandHandler(
        IAppointmentRepository appointments, IClinicSettingsRepository settings)
    {
        _appointments = appointments; _settings = settings;
    }

    /// <summary>Validates open hours and overlap (excluding self), then updates StartsAt and Duration.</summary>
    /// <param name="request">Reschedule data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Appointment {request.Id} not found.");

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new ConflictException("Cannot reschedule a completed or cancelled appointment.");

        var clinic = await _settings.GetSingletonAsync(cancellationToken);
        var day = (int)request.StartsAt.DayOfWeek;
        var openDays = clinic.OpenDays.Split(',').Select(int.Parse).ToHashSet();
        if (!openDays.Contains(day))
            throw new ConflictException("The clinic is not open on that day.");

        var startTime = TimeOnly.FromDateTime(request.StartsAt);
        var endTime   = startTime.AddMinutes(request.DurationMinutes);
        if (startTime < clinic.OpenTime || endTime > clinic.CloseTime)
            throw new ConflictException("The appointment falls outside clinic open hours.");

        var date     = DateOnly.FromDateTime(request.StartsAt);
        var existing = await _appointments.GetByDoctorAndDateAsync(appointment.DoctorId, date, cancellationToken);
        var endsAt   = request.StartsAt.AddMinutes(request.DurationMinutes);
        var conflict = existing.FirstOrDefault(a =>
            a.Id != request.Id &&
            a.Status != AppointmentStatus.Cancelled &&
            request.StartsAt < a.StartsAt.AddMinutes(a.DurationMinutes) &&
            endsAt > a.StartsAt);
        if (conflict is not null)
            throw new ConflictException("The doctor already has an appointment at that time.");

        appointment.StartsAt        = request.StartsAt;
        appointment.DurationMinutes = request.DurationMinutes;
        _appointments.Update(appointment);
        await _appointments.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 4: Create UpdateAppointmentStatus command + handler**

`backend/src/CliniSys.Application/Commands/Appointments/UpdateAppointmentStatus/UpdateAppointmentStatusCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Domain.Enums;
using MediatR;

namespace CliniSys.Application.Commands.Appointments.UpdateAppointmentStatus;

/// <summary>Command to transition an appointment to a new status.</summary>
/// <param name="Id">Appointment identifier.</param>
/// <param name="Status">Target status.</param>
public record UpdateAppointmentStatusCommand(Guid Id, AppointmentStatus Status) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Appointments/UpdateAppointmentStatus/UpdateAppointmentStatusCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Enums;
using MediatR;

namespace CliniSys.Application.Commands.Appointments.UpdateAppointmentStatus;

/// <summary>Handler for <see cref="UpdateAppointmentStatusCommand"/>. Enforces valid status transitions.</summary>
public class UpdateAppointmentStatusCommandHandler : ICommandHandler<UpdateAppointmentStatusCommand, Unit>
{
    private readonly IAppointmentRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Appointment repository.</param>
    public UpdateAppointmentStatusCommandHandler(IAppointmentRepository repo) => _repo = repo;

    /// <summary>Validates the transition and updates the status.</summary>
    /// <param name="request">Status update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdateAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Appointment {request.Id} not found.");

        var valid = (appointment.Status, request.Status) switch
        {
            (AppointmentStatus.Scheduled,  AppointmentStatus.Confirmed)  => true,
            (AppointmentStatus.Scheduled,  AppointmentStatus.Cancelled)  => true,
            (AppointmentStatus.Confirmed,  AppointmentStatus.Completed)  => true,
            (AppointmentStatus.Confirmed,  AppointmentStatus.Cancelled)  => true,
            (AppointmentStatus.Confirmed,  AppointmentStatus.NoShow)     => true,
            (AppointmentStatus.Scheduled,  AppointmentStatus.NoShow)     => true,
            _ => false
        };

        if (!valid)
            throw new ConflictException(
                $"Cannot transition from {appointment.Status} to {request.Status}.");

        appointment.Status = request.Status;
        _repo.Update(appointment);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 5: Create request models and controller**

`backend/src/CliniSys.Api/Requests/Appointments/CreateAppointmentRequest.cs`:
```csharp
namespace CliniSys.Api.Requests.Appointments;

/// <summary>HTTP body for POST /api/appointments.</summary>
/// <param name="PatientId">Patient identifier.</param>
/// <param name="DoctorId">Doctor identifier.</param>
/// <param name="StartsAt">UTC start time.</param>
/// <param name="DurationMinutes">Duration in minutes.</param>
/// <param name="Notes">Optional notes.</param>
public record CreateAppointmentRequest(Guid PatientId, Guid DoctorId,
    DateTime StartsAt, int DurationMinutes, string? Notes);
```

`backend/src/CliniSys.Api/Requests/Appointments/RescheduleAppointmentRequest.cs`:
```csharp
namespace CliniSys.Api.Requests.Appointments;

/// <summary>HTTP body for PUT /api/appointments/{id}.</summary>
/// <param name="StartsAt">New UTC start time.</param>
/// <param name="DurationMinutes">New duration in minutes.</param>
public record RescheduleAppointmentRequest(DateTime StartsAt, int DurationMinutes);
```

`backend/src/CliniSys.Api/Requests/Appointments/UpdateAppointmentStatusRequest.cs`:
```csharp
using CliniSys.Domain.Enums;

namespace CliniSys.Api.Requests.Appointments;

/// <summary>HTTP body for PATCH /api/appointments/{id}/status.</summary>
/// <param name="Status">Target appointment status.</param>
public record UpdateAppointmentStatusRequest(AppointmentStatus Status);
```

`backend/src/CliniSys.Api/Controllers/AppointmentsController.cs`:
```csharp
using System.Security.Claims;
using CliniSys.Api.Requests.Appointments;
using CliniSys.Application.Commands.Appointments.CreateAppointment;
using CliniSys.Application.Commands.Appointments.RescheduleAppointment;
using CliniSys.Application.Commands.Appointments.UpdateAppointmentStatus;
using CliniSys.Application.Queries.Appointments.GetAppointments;
using CliniSys.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Endpoints for managing appointments.</summary>
[ApiController, Route("api/appointments"), Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public AppointmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns appointments. Doctors are restricted to their own appointments.
    /// Pass startDate+endDate for calendar view (pagination ignored).
    /// </summary>
    /// <param name="doctorId">Optional doctor filter.</param>
    /// <param name="patientId">Optional patient filter.</param>
    /// <param name="date">Optional single-day filter.</param>
    /// <param name="startDate">Calendar range start.</param>
    /// <param name="endDate">Calendar range end.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated or date-range appointment list.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? doctorId, [FromQuery] Guid? patientId,
        [FromQuery] DateOnly? date, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] AppointmentStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var role = User.FindFirstValue("role");
        if (role == "Doctor")
        {
            var doctorIdClaim = User.FindFirstValue("doctorId");
            doctorId = doctorIdClaim is not null ? Guid.Parse(doctorIdClaim) : doctorId;
        }
        return Ok(await _mediator.Send(new GetAppointmentsQuery(
            doctorId, patientId, date, startDate, endDate, status, page, pageSize), ct));
    }

    /// <summary>Schedules a new appointment. Staff/Admin only.</summary>
    /// <param name="request">Appointment data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with the new appointment ID.</returns>
    [HttpPost, Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateAppointmentCommand(
            request.PatientId, request.DoctorId, request.StartsAt,
            request.DurationMinutes, request.Notes), ct);
        return CreatedAtAction(nameof(GetAll), new { }, new { id });
    }

    /// <summary>Reschedules an existing appointment. Staff/Admin only.</summary>
    /// <param name="id">Appointment identifier.</param>
    /// <param name="request">New time and duration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut("{id:guid}"), Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Reschedule(
        [FromRoute] Guid id, [FromBody] RescheduleAppointmentRequest request, CancellationToken ct)
    {
        await _mediator.Send(new RescheduleAppointmentCommand(id, request.StartsAt, request.DurationMinutes), ct);
        return NoContent();
    }

    /// <summary>Updates appointment status.</summary>
    /// <param name="id">Appointment identifier.</param>
    /// <param name="request">Target status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid id, [FromBody] UpdateAppointmentStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateAppointmentStatusCommand(id, request.Status), ct);
        return NoContent();
    }
}
```

- [ ] **Step 6: Verify build and commit**

```bash
cd backend && dotnet build
git add backend/src/CliniSys.Application/Queries/Appointments/ \
         backend/src/CliniSys.Application/Commands/Appointments/ \
         backend/src/CliniSys.Api/Requests/Appointments/ \
         backend/src/CliniSys.Api/Controllers/AppointmentsController.cs
git commit -m "feat: add Appointments feature (list/calendar, create, reschedule, status)"
```

---

### Task 15: Auth + Account Features

**Files:**
- Create: `backend/src/CliniSys.Application/Commands/Auth/ChangePassword/ChangePasswordCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Auth/ChangePassword/ChangePasswordCommandValidator.cs`
- Create: `backend/src/CliniSys.Application/Commands/Auth/ChangePassword/ChangePasswordCommandHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Account/UpdateProfilePicture/UpdateProfilePictureCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Account/UpdateProfilePicture/UpdateProfilePictureCommandValidator.cs`
- Create: `backend/src/CliniSys.Application/Commands/Account/UpdateProfilePicture/UpdateProfilePictureCommandHandler.cs`
- Create: `backend/src/CliniSys.Application/Commands/Account/UpdatePreferences/UpdatePreferencesCommand.cs`
- Create: `backend/src/CliniSys.Application/Commands/Account/UpdatePreferences/UpdatePreferencesCommandValidator.cs`
- Create: `backend/src/CliniSys.Application/Commands/Account/UpdatePreferences/UpdatePreferencesCommandHandler.cs`
- Create: `backend/src/CliniSys.Api/Requests/Auth/ChangePasswordRequest.cs`
- Create: `backend/src/CliniSys.Api/Requests/Account/UpdateProfilePictureRequest.cs`
- Create: `backend/src/CliniSys.Api/Requests/Account/UpdatePreferencesRequest.cs`
- Create: `backend/src/CliniSys.Api/Controllers/AuthController.cs`
- Create: `backend/src/CliniSys.Api/Controllers/AccountController.cs`

**Interfaces:**
- Produces: `POST /api/auth/change-password`, `PATCH /api/account/profile-picture`, `PATCH /api/account/preferences`

- [ ] **Step 1: Create ChangePassword command + validator + handler**

`backend/src/CliniSys.Application/Commands/Auth/ChangePassword/ChangePasswordCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Auth.ChangePassword;

/// <summary>Command for a user to change their own password.</summary>
/// <param name="UserId">The calling user's identifier.</param>
/// <param name="CurrentPassword">Current plain-text password for verification.</param>
/// <param name="NewPassword">New plain-text password.</param>
public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Auth/ChangePassword/ChangePasswordCommandValidator.cs`:
```csharp
using FluentValidation;

namespace CliniSys.Application.Commands.Auth.ChangePassword;

/// <summary>Validates <see cref="ChangePasswordCommand"/>.</summary>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>Defines validation rules.</summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from current password.");
    }
}
```

`backend/src/CliniSys.Application/Commands/Auth/ChangePassword/ChangePasswordCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Auth.ChangePassword;

/// <summary>Handler for <see cref="ChangePasswordCommand"/>.</summary>
public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Unit>
{
    private readonly IIdentityService _identity;
    /// <summary>Initialises the handler.</summary>
    /// <param name="identity">Identity service.</param>
    public ChangePasswordCommandHandler(IIdentityService identity) => _identity = identity;

    /// <summary>Changes the user's password after verifying the current one.</summary>
    /// <param name="request">Password change data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        await _identity.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 2: Create Account commands (UpdateProfilePicture + UpdatePreferences)**

`backend/src/CliniSys.Application/Commands/Account/UpdateProfilePicture/UpdateProfilePictureCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Account.UpdateProfilePicture;

/// <summary>Command to set or clear a user's profile picture.</summary>
/// <param name="UserId">The calling user's identifier.</param>
/// <param name="ProfilePictureBase64">Base64 data URI, or <see langword="null"/> to remove.</param>
public record UpdateProfilePictureCommand(Guid UserId, string? ProfilePictureBase64) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Account/UpdateProfilePicture/UpdateProfilePictureCommandValidator.cs`:
```csharp
using FluentValidation;

namespace CliniSys.Application.Commands.Account.UpdateProfilePicture;

/// <summary>Validates <see cref="UpdateProfilePictureCommand"/>.</summary>
public class UpdateProfilePictureCommandValidator : AbstractValidator<UpdateProfilePictureCommand>
{
    /// <summary>Defines validation rules.</summary>
    public UpdateProfilePictureCommandValidator() =>
        RuleFor(x => x.ProfilePictureBase64)
            .Must(v => v is null || (v.StartsWith("data:image/") && v.IndexOf(',') >= 0
                && (v[(v.IndexOf(',') + 1)..].Length * 3 / 4) <= 512 * 1024))
            .WithMessage("Profile picture must be a valid base64 image data URI (max 512 KB).");
}
```

`backend/src/CliniSys.Application/Commands/Account/UpdateProfilePicture/UpdateProfilePictureCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.Account.UpdateProfilePicture;

/// <summary>Handler for <see cref="UpdateProfilePictureCommand"/>.</summary>
public class UpdateProfilePictureCommandHandler : ICommandHandler<UpdateProfilePictureCommand, Unit>
{
    private readonly IUserRepository _users;
    /// <summary>Initialises the handler.</summary>
    /// <param name="users">User repository.</param>
    public UpdateProfilePictureCommandHandler(IUserRepository users) => _users = users;

    /// <summary>Updates or clears the user's profile picture.</summary>
    /// <param name="request">Picture data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdateProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException($"User {request.UserId} not found.");
        user.ProfilePictureBase64 = request.ProfilePictureBase64;
        _users.Update(user);
        await _users.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

`backend/src/CliniSys.Application/Commands/Account/UpdatePreferences/UpdatePreferencesCommand.cs`:
```csharp
using CliniSys.Application.Common.Interfaces;
using CliniSys.Domain.Enums;
using MediatR;

namespace CliniSys.Application.Commands.Account.UpdatePreferences;

/// <summary>Command to update a user's theme and language preferences.</summary>
/// <param name="UserId">The calling user's identifier.</param>
/// <param name="Theme">Preferred theme.</param>
/// <param name="Language">BCP-47 language tag (en-US, pt-BR, es-ES).</param>
public record UpdatePreferencesCommand(Guid UserId, ThemePreference Theme, string Language) : ICommand<Unit>;
```

`backend/src/CliniSys.Application/Commands/Account/UpdatePreferences/UpdatePreferencesCommandValidator.cs`:
```csharp
using FluentValidation;

namespace CliniSys.Application.Commands.Account.UpdatePreferences;

/// <summary>Validates <see cref="UpdatePreferencesCommand"/>.</summary>
public class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    private static readonly string[] SupportedLanguages = ["en-US", "pt-BR", "es-ES"];
    /// <summary>Defines validation rules.</summary>
    public UpdatePreferencesCommandValidator() =>
        RuleFor(x => x.Language).Must(l => SupportedLanguages.Contains(l))
            .WithMessage("Language must be one of: en-US, pt-BR, es-ES.");
}
```

`backend/src/CliniSys.Application/Commands/Account/UpdatePreferences/UpdatePreferencesCommandHandler.cs`:
```csharp
using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.Account.UpdatePreferences;

/// <summary>Handler for <see cref="UpdatePreferencesCommand"/>.</summary>
public class UpdatePreferencesCommandHandler : ICommandHandler<UpdatePreferencesCommand, Unit>
{
    private readonly IUserRepository _users;
    /// <summary>Initialises the handler.</summary>
    /// <param name="users">User repository.</param>
    public UpdatePreferencesCommandHandler(IUserRepository users) => _users = users;

    /// <summary>Updates the user's theme and language preferences.</summary>
    /// <param name="request">New preferences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException($"User {request.UserId} not found.");
        user.ThemePreference    = request.Theme;
        user.LanguagePreference = request.Language;
        _users.Update(user);
        await _users.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 3: Create request models and controllers**

`backend/src/CliniSys.Api/Requests/Auth/ChangePasswordRequest.cs`:
```csharp
namespace CliniSys.Api.Requests.Auth;

/// <summary>HTTP body for POST /api/auth/change-password.</summary>
/// <param name="CurrentPassword">Current password for verification.</param>
/// <param name="NewPassword">New password to set.</param>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
```

`backend/src/CliniSys.Api/Requests/Account/UpdateProfilePictureRequest.cs`:
```csharp
namespace CliniSys.Api.Requests.Account;

/// <summary>HTTP body for PATCH /api/account/profile-picture.</summary>
/// <param name="ProfilePictureBase64">Base64 data URI or <see langword="null"/> to remove.</param>
public record UpdateProfilePictureRequest(string? ProfilePictureBase64);
```

`backend/src/CliniSys.Api/Requests/Account/UpdatePreferencesRequest.cs`:
```csharp
using CliniSys.Domain.Enums;

namespace CliniSys.Api.Requests.Account;

/// <summary>HTTP body for PATCH /api/account/preferences.</summary>
/// <param name="Theme">Preferred theme.</param>
/// <param name="Language">BCP-47 language tag.</param>
public record UpdatePreferencesRequest(ThemePreference Theme, string Language);
```

`backend/src/CliniSys.Api/Controllers/AuthController.cs`:
```csharp
using System.Security.Claims;
using CliniSys.Api.Requests.Auth;
using CliniSys.Application.Commands.Auth.ChangePassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Auth endpoints beyond the OpenIddict token endpoint.</summary>
[ApiController, Route("api/auth"), Authorize]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>Allows the authenticated user to change their own password.</summary>
    /// <param name="request">Current and new passwords.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);
        await _mediator.Send(new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword), ct);
        return NoContent();
    }
}
```

`backend/src/CliniSys.Api/Controllers/AccountController.cs`:
```csharp
using System.Security.Claims;
using CliniSys.Api.Requests.Account;
using CliniSys.Application.Commands.Account.UpdatePreferences;
using CliniSys.Application.Commands.Account.UpdateProfilePicture;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniSys.Api.Controllers;

/// <summary>Self-service account endpoints available to all authenticated roles.</summary>
[ApiController, Route("api/account"), Authorize]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;
    /// <summary>Initialises the controller.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public AccountController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    /// <summary>Sets or removes the authenticated user's profile picture.</summary>
    /// <param name="request">Base64 data URI or null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("profile-picture")]
    public async Task<IActionResult> UpdateProfilePicture(
        [FromBody] UpdateProfilePictureRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateProfilePictureCommand(CurrentUserId, request.ProfilePictureBase64), ct);
        return NoContent();
    }

    /// <summary>Updates the authenticated user's theme and language preferences.</summary>
    /// <param name="request">New preferences.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("preferences")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdatePreferencesCommand(CurrentUserId, request.Theme, request.Language), ct);
        return NoContent();
    }
}
```

- [ ] **Step 4: Verify build and commit**

```bash
cd backend && dotnet build
git add backend/src/CliniSys.Application/Commands/Auth/ \
         backend/src/CliniSys.Application/Commands/Account/ \
         backend/src/CliniSys.Api/Requests/Auth/ \
         backend/src/CliniSys.Api/Requests/Account/ \
         backend/src/CliniSys.Api/Controllers/AuthController.cs \
         backend/src/CliniSys.Api/Controllers/AccountController.cs
git commit -m "feat: add Auth (change-password) and Account (profile-picture, preferences) features"
```

---

### Task 16: Localization

**Files:**
- Create: `backend/src/CliniSys.Application/Common/Interfaces/IMessageLocalizer.cs`
- Create: `backend/src/CliniSys.Application/Locales/en-US.json`
- Create: `backend/src/CliniSys.Application/Locales/pt-BR.json`
- Create: `backend/src/CliniSys.Application/Locales/es-ES.json`
- Create: `backend/src/CliniSys.Infrastructure/Localization/MessageLocalizer.cs`
- Create: `backend/src/CliniSys.Api/Middleware/LocalizationMiddleware.cs`
- Modify: `backend/src/CliniSys.Infrastructure/DependencyInjection.cs`
- Modify: `backend/src/CliniSys.Api/Program.cs`

**Interfaces:**
- Produces: `Accept-Language` header sets `CultureInfo`; FluentValidation error messages use localized strings

- [ ] **Step 1: Create IMessageLocalizer**

`backend/src/CliniSys.Application/Common/Interfaces/IMessageLocalizer.cs`:
```csharp
namespace CliniSys.Application.Common.Interfaces;

/// <summary>
/// Provides localized user-facing messages for the current request culture.
/// Used by FluentValidation validators to return translated error messages.
/// </summary>
public interface IMessageLocalizer
{
    /// <summary>Returns the localized string for the given dot-separated key.</summary>
    /// <param name="key">Dot-separated translation key (e.g. <c>validation.required</c>).</param>
    /// <returns>Localized string, or the key itself if not found.</returns>
    string this[string key] { get; }
}
```

- [ ] **Step 2: Create locale JSON files**

`backend/src/CliniSys.Application/Locales/en-US.json`:
```json
{
  "validation": {
    "required": "This field is required.",
    "invalidEmail": "Invalid email address.",
    "minLength": "Must be at least {0} characters.",
    "maxLength": "Must be at most {0} characters.",
    "invalidDate": "Invalid date.",
    "pageSizeExceeded": "PageSize cannot exceed 100.",
    "invalidImage": "Must be a valid base64 image data URI (max 512 KB).",
    "invalidLanguage": "Language must be one of: en-US, pt-BR, es-ES.",
    "passwordMismatch": "New password must differ from the current password.",
    "appointmentPast": "Appointment start time must be in the future.",
    "invalidDuration": "Duration must be between 5 and 480 minutes."
  }
}
```

`backend/src/CliniSys.Application/Locales/pt-BR.json`:
```json
{
  "validation": {
    "required": "Este campo é obrigatório.",
    "invalidEmail": "Endereço de e-mail inválido.",
    "minLength": "Deve ter pelo menos {0} caracteres.",
    "maxLength": "Deve ter no máximo {0} caracteres.",
    "invalidDate": "Data inválida.",
    "pageSizeExceeded": "PageSize não pode exceder 100.",
    "invalidImage": "Deve ser uma URI de dados de imagem base64 válida (máx. 512 KB).",
    "invalidLanguage": "O idioma deve ser um dos: en-US, pt-BR, es-ES.",
    "passwordMismatch": "A nova senha deve ser diferente da senha atual.",
    "appointmentPast": "O horário de início da consulta deve ser no futuro.",
    "invalidDuration": "A duração deve ser entre 5 e 480 minutos."
  }
}
```

`backend/src/CliniSys.Application/Locales/es-ES.json`:
```json
{
  "validation": {
    "required": "Este campo es obligatorio.",
    "invalidEmail": "Dirección de correo electrónico inválida.",
    "minLength": "Debe tener al menos {0} caracteres.",
    "maxLength": "Debe tener como máximo {0} caracteres.",
    "invalidDate": "Fecha inválida.",
    "pageSizeExceeded": "PageSize no puede superar 100.",
    "invalidImage": "Debe ser un URI de datos de imagen base64 válido (máx. 512 KB).",
    "invalidLanguage": "El idioma debe ser uno de: en-US, pt-BR, es-ES.",
    "passwordMismatch": "La nueva contraseña debe ser diferente de la actual.",
    "appointmentPast": "La hora de inicio de la cita debe ser en el futuro.",
    "invalidDuration": "La duración debe estar entre 5 y 480 minutos."
  }
}
```

Mark the JSON files as EmbeddedResource in `CliniSys.Application.csproj`:
```xml
<ItemGroup>
  <EmbeddedResource Include="Locales\*.json" />
</ItemGroup>
```

- [ ] **Step 3: Implement MessageLocalizer in Infrastructure**

`backend/src/CliniSys.Infrastructure/Localization/MessageLocalizer.cs`:
```csharp
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using CliniSys.Application.Common.Interfaces;

namespace CliniSys.Infrastructure.Localization;

internal class MessageLocalizer : IMessageLocalizer
{
    private static readonly string[] SupportedLocales = ["en-US", "pt-BR", "es-ES"];
    private readonly Dictionary<string, JsonElement> _messages;

    public MessageLocalizer()
    {
        var locale = CultureInfo.CurrentUICulture.Name;
        if (!SupportedLocales.Contains(locale)) locale = "en-US";

        var assembly  = typeof(MessageLocalizer).Assembly;
        var appAssembly = Assembly.Load("CliniSys.Application");
        var resourceName = $"CliniSys.Application.Locales.{locale}.json";

        using var stream = appAssembly.GetManifestResourceStream(resourceName)
            ?? appAssembly.GetManifestResourceStream("CliniSys.Application.Locales.en-US.json")!;
        using var reader = new StreamReader(stream);
        var doc = JsonDocument.Parse(reader.ReadToEnd());
        _messages = FlattenJson(doc.RootElement, string.Empty);
    }

    public string this[string key] =>
        _messages.TryGetValue(key, out var v) ? v.GetString() ?? key : key;

    private static Dictionary<string, JsonElement> FlattenJson(JsonElement element, string prefix)
    {
        var result = new Dictionary<string, JsonElement>();
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var prop in element.EnumerateObject())
            {
                var fullKey = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                foreach (var kv in FlattenJson(prop.Value, fullKey))
                    result[kv.Key] = kv.Value;
            }
        else
            result[prefix] = element;
        return result;
    }
}
```

- [ ] **Step 4: Create LocalizationMiddleware**

`backend/src/CliniSys.Api/Middleware/LocalizationMiddleware.cs`:
```csharp
using System.Globalization;

namespace CliniSys.Api.Middleware;

/// <summary>
/// Sets <see cref="CultureInfo.CurrentCulture"/> and <see cref="CultureInfo.CurrentUICulture"/>
/// from the <c>Accept-Language</c> request header before the MVC pipeline runs.
/// Falls back to <c>en-US</c> for unsupported locales.
/// </summary>
public class LocalizationMiddleware
{
    private static readonly string[] Supported = ["en-US", "pt-BR", "es-ES"];
    private readonly RequestDelegate _next;

    /// <summary>Initialises the middleware.</summary>
    /// <param name="next">Next middleware in the pipeline.</param>
    public LocalizationMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Resolves the locale and sets thread culture, then continues.</summary>
    /// <param name="context">HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers.AcceptLanguage.FirstOrDefault() ?? "en-US";
        var locale = Supported.FirstOrDefault(s => header.Contains(s)) ?? "en-US";
        var culture = new CultureInfo(locale);
        CultureInfo.CurrentCulture   = culture;
        CultureInfo.CurrentUICulture = culture;
        await _next(context);
    }
}
```

- [ ] **Step 5: Register IMessageLocalizer and add LocalizationMiddleware**

In `CliniSys.Infrastructure/DependencyInjection.cs`, add before the `return services;` line:
```csharp
services.AddScoped<IMessageLocalizer, MessageLocalizer>();
```

In `CliniSys.Api/Program.cs`, add after `app.UseMiddleware<ExceptionMiddleware>();`:
```csharp
app.UseMiddleware<LocalizationMiddleware>();
```

- [ ] **Step 6: Verify build and commit**

```bash
cd backend && dotnet build
git add backend/src/CliniSys.Application/Common/Interfaces/IMessageLocalizer.cs \
         backend/src/CliniSys.Application/Locales/ \
         backend/src/CliniSys.Infrastructure/Localization/ \
         backend/src/CliniSys.Api/Middleware/LocalizationMiddleware.cs
git commit -m "feat: add backend localization (Accept-Language middleware + IMessageLocalizer)"
```

---

### Task 17: EF Core Migrations + Seed Data

**Files:**
- Create: `backend/src/CliniSys.Infrastructure/Persistence/Migrations/` (generated)
- Modify: `backend/src/CliniSys.Api/Program.cs`

**Interfaces:**
- Produces: initial migration covering all tables; auto-apply on startup; default Admin user seeded

- [ ] **Step 1: Ensure Docker Postgres is running**

```bash
docker compose up -d postgres
```

Wait for: `postgres` container health check to pass (`pg_isready`).

- [ ] **Step 2: Create initial migration**

```bash
cd backend
dotnet ef migrations add InitialCreate \
  --project src/CliniSys.Infrastructure \
  --startup-project src/CliniSys.Api \
  --output-dir Persistence/Migrations
```

Expected: migration files created under `src/CliniSys.Infrastructure/Persistence/Migrations/`.

- [ ] **Step 3: Apply migration to verify**

```bash
cd backend
dotnet ef database update \
  --project src/CliniSys.Infrastructure \
  --startup-project src/CliniSys.Api
```

Expected: `Done.`

- [ ] **Step 4: Add startup migration + seed to Program.cs**

Add the following block just before `app.Run()` in `backend/src/CliniSys.Api/Program.cs`:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CliniSys.Infrastructure.Persistence.AppDbContext>();
    await db.Database.MigrateAsync();

    var userManager = scope.ServiceProvider
        .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<CliniSys.Domain.Entities.ApplicationUser>>();

    const string adminEmail = "admin@clinisys.local";
    if (await userManager.FindByNameAsync(adminEmail) is null)
    {
        var admin = new CliniSys.Domain.Entities.ApplicationUser
        {
            Id       = Guid.NewGuid(),
            UserName = adminEmail,
            Email    = adminEmail,
            FullName = "System Administrator",
            Role     = CliniSys.Domain.Enums.Role.Admin
        };
        await userManager.CreateAsync(admin, "Admin@12345");
    }
}
```

- [ ] **Step 5: Verify startup and seed**

```bash
cd backend && dotnet run --project src/CliniSys.Api
```

Expected: API starts at `http://localhost:5000`; no migration errors; admin user created in DB.
Test: `POST http://localhost:5000/connect/token` with `grant_type=password&username=admin@clinisys.local&password=Admin@12345&scope=openid` → returns `{ access_token, ... }`.

- [ ] **Step 6: Commit**

```bash
git add backend/src/CliniSys.Infrastructure/Persistence/Migrations/ \
         backend/src/CliniSys.Api/Program.cs
git commit -m "feat: add EF Core migrations and startup seed (admin user)"
```
