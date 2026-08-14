using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Queries.Patients.GetPatients;

namespace CliniSys.Application.Queries.Patients.GetPatientById;

/// <summary>Handler for <see cref="GetPatientByIdQuery"/>.</summary>
public class GetPatientByIdQueryHandler : IQueryHandler<GetPatientByIdQuery, PatientModel?>
{
    private readonly IPatientRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Patient repository.</param>
    public GetPatientByIdQueryHandler(IPatientRepository repo) => _repo = repo;

    /// <summary>Returns the patient with the given ID, or <see langword="null"/> if none exists.</summary>
    /// <param name="request">Query with the patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The patient, or <see langword="null"/>.</returns>
    public async Task<PatientModel?> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await _repo.GetByIdAsync(request.Id, cancellationToken);
        return patient is null
            ? null
            : new PatientModel(patient.Id, patient.FullName, patient.DateOfBirth,
                patient.Phone, patient.Email, patient.Notes, patient.IsActive);
    }
}
