using CliniSys.Application.Common.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext context) { _context = context; _set = context.Set<T>(); }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _set.FindAsync([id], ct).AsTask();

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await _set.AddAsync(entity, ct);

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
