namespace Application.Interfaces.Repositories;

/// <summary>
/// Generic persistence contract for aggregate roots.
/// </summary>
public interface IRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, bool trackChanges = false, CancellationToken ct = default);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
