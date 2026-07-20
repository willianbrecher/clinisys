using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Queries.Appointments.GetAppointments;

/// <summary>
/// Query for appointments. Supports list view (paginated) and calendar view (date range, no pagination).
/// When <see cref="StartDate"/> and <see cref="EndDate"/> are both provided, pagination is ignored.
/// </summary>
/// <param name="DoctorId">Optional doctor filter.</param>
/// <param name="PatientId">Optional patient filter.</param>
/// <param name="Date">Optional single-day filter.</param>
/// <param name="StartDate">Calendar range start (UTC).</param>
/// <param name="EndDate">Calendar range end (UTC).</param>
/// <param name="Status">Optional status filter.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 100).</param>
public record GetAppointmentsQuery(
    Guid? DoctorId = null, Guid? PatientId = null, DateOnly? Date = null,
    DateTime? StartDate = null, DateTime? EndDate = null,
    AppointmentStatus? Status = null,
    int Page = 1, int PageSize = 20) : IPagedQuery<AppointmentModel>;
