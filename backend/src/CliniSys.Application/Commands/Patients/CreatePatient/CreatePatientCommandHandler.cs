using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;

namespace CliniSys.Application.Commands.Patients.CreatePatient;

/// <summary>Handler for <see cref="CreatePatientCommand"/>.</summary>
public class CreatePatientCommandHandler : ICommandHandler<CreatePatientCommand, Guid>
{
    private readonly IPatientRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Patient repository.</param>
    public CreatePatientCommandHandler(IPatientRepository repo) => _repo = repo;

    /// <summary>Creates a new patient record and returns its ID.</summary>
    /// <param name="request">Patient creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new patient's <see cref="Guid"/>.</returns>
    public async Task<Guid> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(), FullName = request.FullName,
            DateOfBirth = request.DateOfBirth, Phone = request.Phone,
            Email = request.Email, Notes = request.Notes,
            HealthPlanId = request.HealthPlanId, HealthPlanNumber = request.HealthPlanNumber
        };
        await _repo.AddAsync(patient, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);
        return patient.Id;
    }
}
