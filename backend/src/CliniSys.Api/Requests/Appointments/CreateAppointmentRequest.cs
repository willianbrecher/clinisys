namespace CliniSys.Api.Requests.Appointments;

/// <summary>HTTP body for POST /api/appointments.</summary>
/// <param name="PatientId">Patient identifier.</param>
/// <param name="DoctorId">Doctor identifier.</param>
/// <param name="StartsAt">UTC start time.</param>
/// <param name="DurationMinutes">Duration in minutes.</param>
/// <param name="Notes">Optional notes.</param>
public record CreateAppointmentRequest(Guid PatientId, Guid DoctorId,
    DateTime StartsAt, int DurationMinutes, string? Notes);
