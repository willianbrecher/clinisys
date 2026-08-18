using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;

namespace CliniSys.Application.Commands.HealthPlans.CreateHealthPlan;

/// <summary>Handler for <see cref="CreateHealthPlanCommand"/>.</summary>
public class CreateHealthPlanCommandHandler : ICommandHandler<CreateHealthPlanCommand, Guid>
{
    private readonly IHealthPlanRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Health plan repository.</param>
    public CreateHealthPlanCommandHandler(IHealthPlanRepository repo) => _repo = repo;

    /// <summary>Creates a new health plan record and returns its ID.</summary>
    /// <param name="request">Health plan creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new health plan's <see cref="Guid"/>.</returns>
    public async Task<Guid> Handle(CreateHealthPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = new HealthPlan { Id = Guid.NewGuid(), Name = request.Name, Notes = request.Notes };
        await _repo.AddAsync(plan, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);
        return plan.Id;
    }
}
