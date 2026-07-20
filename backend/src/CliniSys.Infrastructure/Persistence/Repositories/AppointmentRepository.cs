using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(AppDbContext context) : base(context) { }

    public async Task<List<Appointment>> GetByDoctorAndDateAsync(
        Guid doctorId, DateOnly date, CancellationToken ct = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end   = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        return await _set
            .Where(a => a.DoctorId == doctorId
                     && a.Status != AppointmentStatus.Cancelled
                     && a.StartsAt >= start && a.StartsAt <= end)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Appointment>> GetPagedAsync(
        Guid? doctorId, Guid? patientId, DateOnly? date,
        DateTime? startDate, DateTime? endDate, AppointmentStatus? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _set
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .AsQueryable();

        if (doctorId.HasValue)  query = query.Where(a => a.DoctorId  == doctorId);
        if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId);
        if (status.HasValue)    query = query.Where(a => a.Status    == status);

        if (startDate.HasValue && endDate.HasValue)
        {
            query = query.Where(a => a.StartsAt >= startDate && a.StartsAt <= endDate);
            var all = await query.OrderBy(a => a.StartsAt).ToListAsync(ct);
            return new PagedResult<Appointment>(all, 1, all.Count, all.Count, 1);
        }

        if (date.HasValue)
        {
            var s = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var e = date.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(a => a.StartsAt >= s && a.StartsAt <= e);
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(a => a.StartsAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Appointment>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
