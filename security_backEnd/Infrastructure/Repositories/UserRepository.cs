using Application.Interfaces.Repositories;
using Domain.Entities.Auth;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// User repository implementation.
/// </summary>
public sealed class UserRepository : RepositoryBase<User, Guid>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db)
    {
    }

    public Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default)
        => Set.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userName, ct);
}
