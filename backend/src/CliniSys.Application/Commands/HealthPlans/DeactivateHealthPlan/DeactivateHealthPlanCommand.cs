using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.HealthPlans.DeactivateHealthPlan;

/// <summary>Command to soft-delete a health plan (sets IsActive = false).</summary>
/// <param name="Id">Health plan identifier.</param>
public record DeactivateHealthPlanCommand(Guid Id) : ICommand<Unit>;
