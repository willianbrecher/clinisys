using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.Doctors.UpdateDoctor;

/// <summary>Handler for <see cref="UpdateDoctorCommand"/>.</summary>
public class UpdateDoctorCommandHandler : ICommandHandler<UpdateDoctorCommand, Unit>
{
    private readonly IDoctorRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Doctor repository.</param>
    public UpdateDoctorCommandHandler(IDoctorRepository repo) => _repo = repo;

    /// <summary>Updates the doctor's specialty.</summary>
    /// <param name="request">Update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdateDoctorCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Doctor {request.Id} not found.");
        doctor.Specialty = request.Specialty;
        _repo.Update(doctor);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
