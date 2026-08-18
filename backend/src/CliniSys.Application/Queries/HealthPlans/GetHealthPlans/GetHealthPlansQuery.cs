using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Models;

namespace CliniSys.Application.Queries.HealthPlans.GetHealthPlans;

/// <summary>Query to retrieve a paginated, searchable list of active health plans.</summary>
/// <param name="Search">Optional case-insensitive name filter.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 100).</param>
public record GetHealthPlansQuery(string? Search = null, int Page = 1, int PageSize = 20)
    : IPagedQuery<HealthPlanModel>;
