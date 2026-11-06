using Application.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Shared EF repository helpers.
/// </summary>
public abstract class RepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly AppDbContext Db;

    protected RepositoryBase(AppDbContext db)
    {
        Db = db;
    }

    protected DbSet<TEntity> Set => Db.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, bool trackChanges = false, CancellationToken ct = default)
    {
        if (trackChanges)
            return await Set.FindAsync([id], ct);

        return await Set.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<TKey>(e, "Id")!.Equals(id), ct);
    }

    public void Add(TEntity entity) => Set.Add(entity);
    public void Update(TEntity entity) => Set.Update(entity);
    public void Remove(TEntity entity) => Set.Remove(entity);
}
