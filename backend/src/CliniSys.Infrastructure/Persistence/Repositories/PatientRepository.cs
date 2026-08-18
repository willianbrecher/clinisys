using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class PatientRepository : Repository<Patient>, IPatientRepository
{
    public PatientRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<Patient>> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _set.Include(p => p.HealthPlan).Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.FullName, $"%{search}%"));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Patient>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    public Task<Patient?> GetByIdWithHealthPlanAsync(Guid id, CancellationToken ct = default) =>
        _set.Include(p => p.HealthPlan).FirstOrDefaultAsync(p => p.Id == id, ct);
}
