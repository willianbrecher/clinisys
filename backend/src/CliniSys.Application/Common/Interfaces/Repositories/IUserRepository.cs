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
