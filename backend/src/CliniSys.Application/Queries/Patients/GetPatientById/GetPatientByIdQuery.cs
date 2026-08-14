using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Queries.Patients.GetPatients;

namespace CliniSys.Application.Queries.Patients.GetPatientById;

/// <summary>Query to fetch a single patient by ID.</summary>
/// <param name="Id">Patient identifier.</param>
public record GetPatientByIdQuery(Guid Id) : IQuery<PatientModel?>;
