using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Enums;
using MediatR;

namespace CliniSys.Application.Commands.Appointments.UpdateAppointmentStatus;

/// <summary>Handler for <see cref="UpdateAppointmentStatusCommand"/>. Enforces valid status transitions.</summary>
public class UpdateAppointmentStatusCommandHandler : ICommandHandler<UpdateAppointmentStatusCommand, Unit>
{
    private readonly IAppointmentRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Appointment repository.</param>
    public UpdateAppointmentStatusCommandHandler(IAppointmentRepository repo) => _repo = repo;

    /// <summary>Validates the transition and updates the status.</summary>
    /// <param name="request">Status update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdateAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Appointment {request.Id} not found.");

        var valid = (appointment.Status, request.Status) switch
        {
            var (from, to) when from == to                               => true, // no-op
            (AppointmentStatus.Scheduled,  AppointmentStatus.Confirmed)  => true,
            (AppointmentStatus.Scheduled,  AppointmentStatus.Cancelled)  => true,
            (AppointmentStatus.Scheduled,  AppointmentStatus.Completed)  => true,
            (AppointmentStatus.Confirmed,  AppointmentStatus.Completed)  => true,
            (AppointmentStatus.Confirmed,  AppointmentStatus.Cancelled)  => true,
            (AppointmentStatus.Confirmed,  AppointmentStatus.NoShow)     => true,
            (AppointmentStatus.Scheduled,  AppointmentStatus.NoShow)     => true,
            _ => false
        };

        if (!valid)
            throw new ConflictException(
                $"Cannot transition from {appointment.Status} to {request.Status}.");

        appointment.Status = request.Status;
        _repo.Update(appointment);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
