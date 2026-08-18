using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Queries.HealthPlans.GetHealthPlans;

namespace CliniSys.Application.Queries.HealthPlans.GetHealthPlanById;

/// <summary>Handler for <see cref="GetHealthPlanByIdQuery"/>.</summary>
public class GetHealthPlanByIdQueryHandler : IQueryHandler<GetHealthPlanByIdQuery, HealthPlanModel?>
{
    private readonly IHealthPlanRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Health plan repository.</param>
    public GetHealthPlanByIdQueryHandler(IHealthPlanRepository repo) => _repo = repo;

    /// <summary>Returns the health plan with the given ID, or <see langword="null"/> if none exists.</summary>
    /// <param name="request">Query with the health plan ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health plan, or <see langword="null"/>.</returns>
    public async Task<HealthPlanModel?> Handle(GetHealthPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _repo.GetByIdAsync(request.Id, cancellationToken);
        return plan is null ? null : new HealthPlanModel(plan.Id, plan.Name, plan.Notes, plan.IsActive);
    }
}
