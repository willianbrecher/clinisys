using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.HealthPlans.DeactivateHealthPlan;

/// <summary>Handler for <see cref="DeactivateHealthPlanCommand"/>.</summary>
public class DeactivateHealthPlanCommandHandler : ICommandHandler<DeactivateHealthPlanCommand, Unit>
{
    private readonly IHealthPlanRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Health plan repository.</param>
    public DeactivateHealthPlanCommandHandler(IHealthPlanRepository repo) => _repo = repo;

    /// <summary>Sets the health plan's IsActive flag to false.</summary>
    /// <param name="request">Deactivation command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(DeactivateHealthPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Health plan {request.Id} not found.");
        plan.IsActive = false;
        _repo.Update(plan);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
