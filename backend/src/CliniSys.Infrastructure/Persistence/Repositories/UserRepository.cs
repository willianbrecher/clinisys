using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class UserRepository : Repository<ApplicationUser>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _set.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<PagedResult<ApplicationUser>> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var total = await _set.CountAsync(ct);
        var items = await _set.OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<ApplicationUser>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
