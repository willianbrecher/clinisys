using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Patients.DeactivatePatient;

/// <summary>Command to soft-delete a patient (sets IsActive = false).</summary>
/// <param name="Id">Patient identifier.</param>
public record DeactivatePatientCommand(Guid Id) : ICommand<Unit>;
