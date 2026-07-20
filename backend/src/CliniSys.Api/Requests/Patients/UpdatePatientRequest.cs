namespace CliniSys.Api.Requests.Patients;

/// <summary>HTTP body for PUT /api/patients/{id}.</summary>
/// <param name="FullName">Updated full name.</param>
/// <param name="DateOfBirth">Updated date of birth.</param>
/// <param name="Phone">Updated phone.</param>
/// <param name="Email">Updated optional email.</param>
/// <param name="Notes">Updated optional notes.</param>
public record UpdatePatientRequest(string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes);
