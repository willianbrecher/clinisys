using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Appointments.RescheduleAppointment;

/// <summary>Command to reschedule an existing appointment to a new time.</summary>
/// <param name="Id">Appointment identifier.</param>
/// <param name="StartsAt">New UTC start time.</param>
/// <param name="DurationMinutes">New duration in minutes.</param>
public record RescheduleAppointmentCommand(Guid Id, DateTime StartsAt, int DurationMinutes) : ICommand<Unit>;
