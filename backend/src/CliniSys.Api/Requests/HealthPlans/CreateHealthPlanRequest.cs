namespace CliniSys.Api.Requests.HealthPlans;

/// <summary>HTTP body for POST /api/health-plans.</summary>
/// <param name="Name">Plan name.</param>
/// <param name="Notes">Optional notes.</param>
public record CreateHealthPlanRequest(string Name, string? Notes);
