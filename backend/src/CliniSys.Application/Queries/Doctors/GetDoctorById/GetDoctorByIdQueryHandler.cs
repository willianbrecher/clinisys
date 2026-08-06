using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Queries.Doctors.GetDoctors;

namespace CliniSys.Application.Queries.Doctors.GetDoctorById;

/// <summary>Handler for <see cref="GetDoctorByIdQuery"/>.</summary>
public class GetDoctorByIdQueryHandler : IQueryHandler<GetDoctorByIdQuery, DoctorModel?>
{
    private readonly IDoctorRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Doctor repository.</param>
    public GetDoctorByIdQueryHandler(IDoctorRepository repo) => _repo = repo;

    /// <summary>Returns the doctor with the given ID, or <see langword="null"/> if none exists.</summary>
    /// <param name="request">Query with the doctor ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The doctor, or <see langword="null"/>.</returns>
    public async Task<DoctorModel?> Handle(GetDoctorByIdQuery request, CancellationToken cancellationToken)
    {
        var doctor = await _repo.GetByIdWithUserAsync(request.Id, cancellationToken);
        return doctor is null
            ? null
            : new DoctorModel(doctor.Id, doctor.UserId, doctor.User.FullName, doctor.User.Email, doctor.Specialty, doctor.IsActive);
    }
}
