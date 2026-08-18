using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using FluentValidation;

namespace CliniSys.Application.Queries.HealthPlans.GetHealthPlans;

/// <summary>Health plan response model.</summary>
/// <param name="Id">Health plan identifier.</param>
/// <param name="Name">Plan name.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="IsActive">Active status.</param>
public record HealthPlanModel(Guid Id, string Name, string? Notes, bool IsActive);

/// <summary>Handler for <see cref="GetHealthPlansQuery"/>.</summary>
public class GetHealthPlansQueryHandler : IQueryHandler<GetHealthPlansQuery, PagedResult<HealthPlanModel>>
{
    private readonly IHealthPlanRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Health plan repository.</param>
    public GetHealthPlansQueryHandler(IHealthPlanRepository repo) => _repo = repo;

    /// <summary>Returns a paginated filtered list of health plans.</summary>
    /// <param name="request">Query with filters and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated health plan list.</returns>
    public async Task<PagedResult<HealthPlanModel>> Handle(
        GetHealthPlansQuery request, CancellationToken cancellationToken)
    {
        if (request.PageSize > 100) throw new ValidationException("PageSize cannot exceed 100.");

        var paged = await _repo.GetPagedAsync(request.Search, request.Page, request.PageSize, cancellationToken);
        var items = paged.Items.Select(p => new HealthPlanModel(p.Id, p.Name, p.Notes, p.IsActive)).ToList();
        return new PagedResult<HealthPlanModel>(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }
}
