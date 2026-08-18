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

    /// <summary>Finds a patient by ID, including the HealthPlan navigation. Returns <see langword="null"/> if none.</summary>
    /// <param name="id">Patient identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Patient?> GetByIdWithHealthPlanAsync(Guid id, CancellationToken ct = default);
}
