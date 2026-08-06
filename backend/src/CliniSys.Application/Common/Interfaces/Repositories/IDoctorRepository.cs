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

    /// <summary>Finds a doctor by ID, including the User navigation. Returns <see langword="null"/> if none.</summary>
    /// <param name="id">Doctor identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Doctor?> GetByIdWithUserAsync(Guid id, CancellationToken ct = default);
}
