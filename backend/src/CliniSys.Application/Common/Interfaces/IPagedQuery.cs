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
