using CliniSys.Application.Common.Interfaces;

namespace CliniSys.Application.Commands.Appointments.CreateAppointment;

/// <summary>Command to schedule a new appointment.</summary>
/// <param name="PatientId">Patient identifier.</param>
/// <param name="DoctorId">Doctor identifier.</param>
/// <param name="StartsAt">UTC start time.</param>
/// <param name="DurationMinutes">Duration in minutes.</param>
/// <param name="Notes">Optional notes.</param>
public record CreateAppointmentCommand(
    Guid PatientId, Guid DoctorId, DateTime StartsAt,
    int DurationMinutes, string? Notes) : ICommand<Guid>;
