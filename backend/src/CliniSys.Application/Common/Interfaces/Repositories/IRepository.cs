namespace CliniSys.Application.Common.Interfaces.Repositories;

/// <summary>Generic CRUD repository base.</summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Finds an entity by primary key. Returns <see langword="null"/> if not found.</summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Stages a new entity for insert (call <see cref="SaveChangesAsync"/> to persist).</summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(T entity, CancellationToken ct = default);

    /// <summary>Marks an entity as modified (call <see cref="SaveChangesAsync"/> to persist).</summary>
    /// <param name="entity">The entity to update.</param>
    void Update(T entity);

    /// <summary>Marks an entity for removal (call <see cref="SaveChangesAsync"/> to persist).</summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(T entity);

    /// <summary>Persists all staged changes to the database.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
