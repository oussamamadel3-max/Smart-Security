using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Interfaces;

/// <summary>
/// Coordinates persistence commits and optional explicit transactions.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
