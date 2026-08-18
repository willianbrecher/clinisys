using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class HealthPlanRepository : Repository<HealthPlan>, IHealthPlanRepository
{
    public HealthPlanRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<HealthPlan>> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _set.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<HealthPlan>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
