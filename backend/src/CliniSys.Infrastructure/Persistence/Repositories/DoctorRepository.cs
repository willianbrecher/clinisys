using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class DoctorRepository : Repository<Doctor>, IDoctorRepository
{
    public DoctorRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<Doctor>> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _set.Include(d => d.User).Where(d => d.IsActive);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(d => d.User.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Doctor>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    public Task<Doctor?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _set.Include(d => d.User).FirstOrDefaultAsync(d => d.UserId == userId, ct);

    public Task<Doctor?> GetByIdWithUserAsync(Guid id, CancellationToken ct = default) =>
        _set.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id, ct);
}
