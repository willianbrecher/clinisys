namespace CliniSys.Api.Requests.Appointments;

/// <summary>HTTP body for PUT /api/appointments/{id}.</summary>
/// <param name="StartsAt">New UTC start time.</param>
/// <param name="DurationMinutes">New duration in minutes.</param>
public record RescheduleAppointmentRequest(DateTime StartsAt, int DurationMinutes);
