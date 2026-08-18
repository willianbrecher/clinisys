using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.Patients.UpdatePatient;

/// <summary>Handler for <see cref="UpdatePatientCommand"/>.</summary>
public class UpdatePatientCommandHandler : ICommandHandler<UpdatePatientCommand, Unit>
{
    private readonly IPatientRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Patient repository.</param>
    public UpdatePatientCommandHandler(IPatientRepository repo) => _repo = repo;

    /// <summary>Updates the patient's details.</summary>
    /// <param name="request">Update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Patient {request.Id} not found.");
        patient.FullName    = request.FullName;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Phone       = request.Phone;
        patient.Email       = request.Email;
        patient.Notes       = request.Notes;
        patient.HealthPlanId     = request.HealthPlanId;
        patient.HealthPlanNumber = request.HealthPlanNumber;
        _repo.Update(patient);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
