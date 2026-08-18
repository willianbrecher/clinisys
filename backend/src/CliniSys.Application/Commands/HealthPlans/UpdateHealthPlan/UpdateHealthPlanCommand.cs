using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.HealthPlans.UpdateHealthPlan;

/// <summary>Command to update an existing health plan.</summary>
/// <param name="Id">Health plan identifier.</param>
/// <param name="Name">Updated plan name.</param>
/// <param name="Notes">Updated optional notes.</param>
public record UpdateHealthPlanCommand(Guid Id, string Name, string? Notes) : ICommand<Unit>;
