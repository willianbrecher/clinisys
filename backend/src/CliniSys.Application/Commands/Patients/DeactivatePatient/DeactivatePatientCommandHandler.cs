using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.Patients.DeactivatePatient;

/// <summary>Handler for <see cref="DeactivatePatientCommand"/>.</summary>
public class DeactivatePatientCommandHandler : ICommandHandler<DeactivatePatientCommand, Unit>
{
    private readonly IPatientRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Patient repository.</param>
    public DeactivatePatientCommandHandler(IPatientRepository repo) => _repo = repo;

    /// <summary>Sets the patient's IsActive flag to false.</summary>
    /// <param name="request">Deactivation command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(DeactivatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Patient {request.Id} not found.");
        patient.IsActive = false;
        _repo.Update(patient);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
