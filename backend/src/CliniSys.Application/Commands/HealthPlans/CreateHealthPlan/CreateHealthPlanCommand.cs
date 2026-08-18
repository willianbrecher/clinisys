using CliniSys.Application.Common.Interfaces;

namespace CliniSys.Application.Commands.HealthPlans.CreateHealthPlan;

/// <summary>Command to register a new health plan.</summary>
/// <param name="Name">Plan name.</param>
/// <param name="Notes">Optional notes.</param>
public record CreateHealthPlanCommand(string Name, string? Notes) : ICommand<Guid>;
