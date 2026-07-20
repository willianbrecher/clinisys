using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using FluentValidation;

namespace CliniSys.Application.Queries.Patients.GetPatients;

/// <summary>Patient response model.</summary>
/// <param name="Id">Patient identifier.</param>
/// <param name="FullName">Full name.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="Phone">Contact phone.</param>
/// <param name="Email">Optional email.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="IsActive">Active status.</param>
public record PatientModel(Guid Id, string FullName, DateOnly DateOfBirth,
    string Phone, string? Email, string? Notes, bool IsActive);

/// <summary>Handler for <see cref="GetPatientsQuery"/>.</summary>
public class GetPatientsQueryHandler : IQueryHandler<GetPatientsQuery, PagedResult<PatientModel>>
{
    private readonly IPatientRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Patient repository.</param>
    public GetPatientsQueryHandler(IPatientRepository repo) => _repo = repo;

    /// <summary>Returns a paginated filtered list of patients.</summary>
    /// <param name="request">Query with filters and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated patient list.</returns>
    public async Task<PagedResult<PatientModel>> Handle(
        GetPatientsQuery request, CancellationToken cancellationToken)
    {
        if (request.PageSize > 100) throw new ValidationException("PageSize cannot exceed 100.");

        var paged = await _repo.GetPagedAsync(request.Search, request.Page, request.PageSize, cancellationToken);
        var items = paged.Items.Select(p =>
            new PatientModel(p.Id, p.FullName, p.DateOfBirth, p.Phone, p.Email, p.Notes, p.IsActive)).ToList();
        return new PagedResult<PatientModel>(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }
}
