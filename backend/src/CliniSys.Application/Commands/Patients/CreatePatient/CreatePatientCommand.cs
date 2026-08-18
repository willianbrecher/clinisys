using CliniSys.Application.Common.Interfaces;

namespace CliniSys.Application.Commands.Patients.CreatePatient;

/// <summary>Command to register a new patient.</summary>
/// <param name="FullName">Patient full name.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="Phone">Contact phone.</param>
/// <param name="Email">Optional email.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="HealthPlanId">Optional linked health plan.</param>
/// <param name="HealthPlanNumber">Optional membership/card number under the linked plan.</param>
public record CreatePatientCommand(string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes,
    Guid? HealthPlanId, string? HealthPlanNumber) : ICommand<Guid>;
