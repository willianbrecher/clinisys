using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Queries.HealthPlans.GetHealthPlans;

namespace CliniSys.Application.Queries.HealthPlans.GetHealthPlanById;

/// <summary>Query to fetch a single health plan by ID.</summary>
/// <param name="Id">Health plan identifier.</param>
public record GetHealthPlanByIdQuery(Guid Id) : IQuery<HealthPlanModel?>;
