using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using FluentValidation;

namespace CliniSys.Application.Queries.Doctors.GetDoctors;

/// <summary>Doctor response model.</summary>
/// <param name="Id">Doctor identifier.</param>
/// <param name="UserId">Linked user identifier.</param>
/// <param name="FullName">Doctor's full name.</param>
/// <param name="Email">Doctor's email.</param>
/// <param name="Specialty">Medical specialty.</param>
/// <param name="IsActive">Active status.</param>
public record DoctorModel(Guid Id, Guid UserId, string FullName, string? Email, string Specialty, bool IsActive);

/// <summary>Handler for <see cref="GetDoctorsQuery"/>.</summary>
public class GetDoctorsQueryHandler : IQueryHandler<GetDoctorsQuery, PagedResult<DoctorModel>>
{
    private readonly IDoctorRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Doctor repository.</param>
    public GetDoctorsQueryHandler(IDoctorRepository repo) => _repo = repo;

    /// <summary>Returns paginated active doctors.</summary>
    /// <param name="request">Query with pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated doctor list.</returns>
    public async Task<PagedResult<DoctorModel>> Handle(
        GetDoctorsQuery request, CancellationToken cancellationToken)
    {
        if (request.PageSize > 100) throw new ValidationException("PageSize cannot exceed 100.");
        var paged = await _repo.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
        var items = paged.Items.Select(d =>
            new DoctorModel(d.Id, d.UserId, d.User.FullName, d.User.Email, d.Specialty, d.IsActive)).ToList();
        return new PagedResult<DoctorModel>(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }
}
