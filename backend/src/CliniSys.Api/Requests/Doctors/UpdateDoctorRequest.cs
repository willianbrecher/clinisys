namespace CliniSys.Api.Requests.Doctors;

/// <summary>HTTP body for PATCH /api/doctors/{id}.</summary>
/// <param name="Specialty">Updated medical specialty.</param>
public record UpdateDoctorRequest(string Specialty);
