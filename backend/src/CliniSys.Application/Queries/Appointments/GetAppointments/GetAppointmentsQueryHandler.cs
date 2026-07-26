using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Enums;
using FluentValidation;

namespace CliniSys.Application.Queries.Appointments.GetAppointments;

/// <summary>Appointment response model.</summary>
/// <param name="Id">Appointment identifier.</param>
/// <param name="PatientId">Patient identifier.</param>
/// <param name="PatientName">Patient full name.</param>
/// <param name="DoctorId">Doctor identifier.</param>
/// <param name="DoctorName">Doctor full name.</param>
/// <param name="StartsAt">UTC start time.</param>
/// <param name="DurationMinutes">Duration in minutes.</param>
/// <param name="Status">Current status.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
public record AppointmentModel(
    Guid Id, Guid PatientId, string PatientName,
    Guid DoctorId, string DoctorName,
    DateTime StartsAt, int DurationMinutes,
    AppointmentStatus Status, string? Notes, DateTime CreatedAt);

/// <summary>Handler for <see cref="GetAppointmentsQuery"/>.</summary>
public class GetAppointmentsQueryHandler : IQueryHandler<GetAppointmentsQuery, PagedResult<AppointmentModel>>
{
    private readonly IAppointmentRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Appointment repository.</param>
    public GetAppointmentsQueryHandler(IAppointmentRepository repo) => _repo = repo;

    /// <summary>Returns paginated or date-range appointments.</summary>
    /// <param name="request">Query filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated appointment list.</returns>
    public async Task<PagedResult<AppointmentModel>> Handle(
        GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        if (request.PageSize > 500) throw new ValidationException("PageSize cannot exceed 500.");

        var paged = await _repo.GetPagedAsync(
            request.DoctorId, request.PatientId, request.Date,
            request.StartDate, request.EndDate, request.Status,
            request.Page, request.PageSize, cancellationToken);

        var items = paged.Items.Select(a => new AppointmentModel(
            a.Id, a.PatientId, a.Patient.FullName,
            a.DoctorId, a.Doctor.User.FullName,
            a.StartsAt, a.DurationMinutes, a.Status, a.Notes, a.CreatedAt)).ToList();

        return new PagedResult<AppointmentModel>(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }
}
