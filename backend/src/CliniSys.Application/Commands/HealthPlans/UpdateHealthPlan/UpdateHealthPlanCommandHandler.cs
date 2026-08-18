using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.HealthPlans.UpdateHealthPlan;

/// <summary>Handler for <see cref="UpdateHealthPlanCommand"/>.</summary>
public class UpdateHealthPlanCommandHandler : ICommandHandler<UpdateHealthPlanCommand, Unit>
{
    private readonly IHealthPlanRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Health plan repository.</param>
    public UpdateHealthPlanCommandHandler(IHealthPlanRepository repo) => _repo = repo;

    /// <summary>Updates the health plan's details.</summary>
    /// <param name="request">Update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdateHealthPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Health plan {request.Id} not found.");
        plan.Name  = request.Name;
        plan.Notes = request.Notes;
        _repo.Update(plan);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
