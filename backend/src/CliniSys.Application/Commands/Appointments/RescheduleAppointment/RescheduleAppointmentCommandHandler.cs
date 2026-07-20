using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Enums;
using MediatR;

namespace CliniSys.Application.Commands.Appointments.RescheduleAppointment;

/// <summary>Handler for <see cref="RescheduleAppointmentCommand"/>.</summary>
public class RescheduleAppointmentCommandHandler : ICommandHandler<RescheduleAppointmentCommand, Unit>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IClinicSettingsRepository _settings;

    /// <summary>Initialises the handler.</summary>
    /// <param name="appointments">Appointment repository.</param>
    /// <param name="settings">Clinic settings repository.</param>
    public RescheduleAppointmentCommandHandler(
        IAppointmentRepository appointments, IClinicSettingsRepository settings)
    {
        _appointments = appointments; _settings = settings;
    }

    /// <summary>Validates open hours and overlap (excluding self), then updates StartsAt and Duration.</summary>
    /// <param name="request">Reschedule data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Appointment {request.Id} not found.");

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new ConflictException("Cannot reschedule a completed or cancelled appointment.");

        var clinic = await _settings.GetSingletonAsync(cancellationToken);
        var day = (int)request.StartsAt.DayOfWeek;
        var openDays = clinic.OpenDays.Split(',').Select(int.Parse).ToHashSet();
        if (!openDays.Contains(day))
            throw new ConflictException("The clinic is not open on that day.");

        var startTime = TimeOnly.FromDateTime(request.StartsAt);
        var endTime   = startTime.AddMinutes(request.DurationMinutes);
        if (startTime < clinic.OpenTime || endTime > clinic.CloseTime)
            throw new ConflictException("The appointment falls outside clinic open hours.");

        var date     = DateOnly.FromDateTime(request.StartsAt);
        var existing = await _appointments.GetByDoctorAndDateAsync(appointment.DoctorId, date, cancellationToken);
        var endsAt   = request.StartsAt.AddMinutes(request.DurationMinutes);
        var conflict = existing.FirstOrDefault(a =>
            a.Id != request.Id &&
            a.Status != AppointmentStatus.Cancelled &&
            request.StartsAt < a.StartsAt.AddMinutes(a.DurationMinutes) &&
            endsAt > a.StartsAt);
        if (conflict is not null)
            throw new ConflictException("The doctor already has an appointment at that time.");

        appointment.StartsAt        = request.StartsAt;
        appointment.DurationMinutes = request.DurationMinutes;
        _appointments.Update(appointment);
        await _appointments.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
