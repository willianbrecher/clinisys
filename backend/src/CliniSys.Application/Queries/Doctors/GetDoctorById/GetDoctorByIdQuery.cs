using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Queries.Doctors.GetDoctors;

namespace CliniSys.Application.Queries.Doctors.GetDoctorById;

/// <summary>Query to fetch a single doctor by ID.</summary>
/// <param name="Id">Doctor identifier.</param>
public record GetDoctorByIdQuery(Guid Id) : IQuery<DoctorModel?>;
