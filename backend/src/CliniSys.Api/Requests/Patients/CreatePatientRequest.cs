namespace CliniSys.Api.Requests.Patients;

/// <summary>HTTP body for POST /api/patients.</summary>
/// <param name="FullName">Patient full name.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="Phone">Contact phone.</param>
/// <param name="Email">Optional email.</param>
/// <param name="Notes">Optional notes.</param>
public record CreatePatientRequest(string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes);
