using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Common.Interfaces.Repositories;

/// <summary>Repository for <see cref="Appointment"/> with filtering and overlap-check support.</summary>
public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>Returns all non-cancelled appointments for a doctor on a specific date (for overlap validation).</summary>
    /// <param name="doctorId">Doctor identifier.</param>
    /// <param name="date">The date to check.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<Appointment>> GetByDoctorAndDateAsync(Guid doctorId, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated list of appointments. When both <paramref name="startDate"/> and
    /// <paramref name="endDate"/> are provided, pagination is ignored and all matching records are returned.
    /// </summary>
    /// <param name="doctorId">Optional doctor filter.</param>
    /// <param name="patientId">Optional patient filter.</param>
    /// <param name="date">Optional single-day filter.</param>
    /// <param name="startDate">Optional range start (calendar view).</param>
    /// <param name="endDate">Optional range end (calendar view).</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated appointments.</returns>
    Task<PagedResult<Appointment>> GetPagedAsync(
        Guid? doctorId, Guid? patientId, DateOnly? date,
        DateTime? startDate, DateTime? endDate, AppointmentStatus? status,
        int page, int pageSize, CancellationToken ct = default);
}
