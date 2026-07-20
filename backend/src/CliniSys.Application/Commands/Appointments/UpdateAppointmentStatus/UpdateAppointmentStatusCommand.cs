using CliniSys.Application.Common.Interfaces;
using CliniSys.Domain.Enums;
using MediatR;

namespace CliniSys.Application.Commands.Appointments.UpdateAppointmentStatus;

/// <summary>Command to transition an appointment to a new status.</summary>
/// <param name="Id">Appointment identifier.</param>
/// <param name="Status">Target status.</param>
public record UpdateAppointmentStatusCommand(Guid Id, AppointmentStatus Status) : ICommand<Unit>;
