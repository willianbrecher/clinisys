using CliniSys.Domain.Enums;

namespace CliniSys.Api.Requests.Appointments;

/// <summary>HTTP body for PATCH /api/appointments/{id}/status.</summary>
/// <param name="Status">Target appointment status.</param>
public record UpdateAppointmentStatusRequest(AppointmentStatus Status);
