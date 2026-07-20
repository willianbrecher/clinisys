using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Commands.Appointments.CreateAppointment;

/// <summary>Handler for <see cref="CreateAppointmentCommand"/>.</summary>
public class CreateAppointmentCommandHandler : ICommandHandler<CreateAppointmentCommand, Guid>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IClinicSettingsRepository _settings;

    /// <summary>Initialises the handler.</summary>
    /// <param name="appointments">Appointment repository.</param>
    /// <param name="settings">Clinic settings repository for open hours validation.</param>
    public CreateAppointmentCommandHandler(
        IAppointmentRepository appointments, IClinicSettingsRepository settings)
    {
        _appointments = appointments; _settings = settings;
    }

    /// <summary>Validates open hours and overlap, then creates the appointment.</summary>
    /// <param name="request">Appointment data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new appointment's <see cref="Guid"/>.</returns>
    public async Task<Guid> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var clinic = await _settings.GetSingletonAsync(cancellationToken);
        ValidateOpenHours(request.StartsAt, request.DurationMinutes, clinic);

        var date = DateOnly.FromDateTime(request.StartsAt);
        var existing = await _appointments.GetByDoctorAndDateAsync(request.DoctorId, date, cancellationToken);
        CheckOverlap(request.StartsAt, request.DurationMinutes, existing, excludeId: null);

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(), PatientId = request.PatientId, DoctorId = request.DoctorId,
            StartsAt = request.StartsAt, DurationMinutes = request.DurationMinutes, Notes = request.Notes
        };
        await _appointments.AddAsync(appointment, cancellationToken);
        await _appointments.SaveChangesAsync(cancellationToken);
        return appointment.Id;
    }

    private static void ValidateOpenHours(DateTime startsAt, int durationMinutes, Domain.Entities.ClinicSettings clinic)
    {
        var day = (int)startsAt.DayOfWeek;
        var openDays = clinic.OpenDays.Split(',').Select(int.Parse).ToHashSet();
        if (!openDays.Contains(day))
            throw new ConflictException("The clinic is not open on that day.");

        var startTime = TimeOnly.FromDateTime(startsAt);
        var endTime   = startTime.AddMinutes(durationMinutes);
        if (startTime < clinic.OpenTime || endTime > clinic.CloseTime)
            throw new ConflictException("The appointment falls outside clinic open hours.");
    }

    private static void CheckOverlap(DateTime startsAt, int durationMinutes,
        List<Appointment> existing, Guid? excludeId)
    {
        var endsAt = startsAt.AddMinutes(durationMinutes);
        var conflict = existing.FirstOrDefault(a =>
            a.Id != excludeId &&
            a.Status != AppointmentStatus.Cancelled &&
            startsAt < a.StartsAt.AddMinutes(a.DurationMinutes) &&
            endsAt   > a.StartsAt);
        if (conflict is not null)
            throw new ConflictException("The doctor already has an appointment at that time.");
    }
}
