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
