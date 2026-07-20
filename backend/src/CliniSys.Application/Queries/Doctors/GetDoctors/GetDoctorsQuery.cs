using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Models;

namespace CliniSys.Application.Queries.Doctors.GetDoctors;

/// <summary>Query to retrieve a paginated list of active doctors.</summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 100).</param>
public record GetDoctorsQuery(int Page = 1, int PageSize = 20) : IPagedQuery<DoctorModel>;
