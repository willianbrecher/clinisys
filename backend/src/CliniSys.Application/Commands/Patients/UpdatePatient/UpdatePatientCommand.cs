using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Patients.UpdatePatient;

/// <summary>Command to update an existing patient.</summary>
/// <param name="Id">Patient identifier.</param>
/// <param name="FullName">Updated full name.</param>
/// <param name="DateOfBirth">Updated date of birth.</param>
/// <param name="Phone">Updated phone.</param>
/// <param name="Email">Updated optional email.</param>
/// <param name="Notes">Updated optional notes.</param>
/// <param name="HealthPlanId">Updated optional linked health plan.</param>
/// <param name="HealthPlanNumber">Updated optional membership/card number under the linked plan.</param>
public record UpdatePatientCommand(Guid Id, string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes,
    Guid? HealthPlanId, string? HealthPlanNumber) : ICommand<Unit>;
