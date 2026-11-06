namespace Application.Interfaces.Repositories;

/// <summary>
/// User persistence contract.
/// </summary>
public interface IUserRepository : IRepository<Domain.Entities.Auth.User, Guid>
{
    Task<Domain.Entities.Auth.User?> FindByUserNameAsync(string userName, CancellationToken ct = default);
}
