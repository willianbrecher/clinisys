namespace CliniSys.Api.Requests.HealthPlans;

/// <summary>HTTP body for PUT /api/health-plans/{id}.</summary>
/// <param name="Name">Updated plan name.</param>
/// <param name="Notes">Updated optional notes.</param>
public record UpdateHealthPlanRequest(string Name, string? Notes);
