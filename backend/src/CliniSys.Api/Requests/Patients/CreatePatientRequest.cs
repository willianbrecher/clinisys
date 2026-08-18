namespace CliniSys.Api.Requests.Patients;

/// <summary>HTTP body for POST /api/patients.</summary>
/// <param name="FullName">Patient full name.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="Phone">Contact phone.</param>
/// <param name="Email">Optional email.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="HealthPlanId">Optional linked health plan.</param>
/// <param name="HealthPlanNumber">Optional membership/card number under the linked plan.</param>
public record CreatePatientRequest(string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes,
    Guid? HealthPlanId, string? HealthPlanNumber);
