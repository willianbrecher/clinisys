using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Doctors.UpdateDoctor;

/// <summary>Command to update a doctor's specialty. Admin only.</summary>
/// <param name="Id">Doctor identifier.</param>
/// <param name="Specialty">Updated specialty.</param>
public record UpdateDoctorCommand(Guid Id, string Specialty) : ICommand<Unit>;
