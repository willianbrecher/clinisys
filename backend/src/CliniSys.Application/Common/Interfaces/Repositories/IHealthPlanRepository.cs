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
