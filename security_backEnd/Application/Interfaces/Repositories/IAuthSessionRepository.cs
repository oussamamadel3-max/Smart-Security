using Domain.Entities.Auth;

namespace Application.Interfaces.Repositories;

public interface IAuthSessionRepository
{
    Task AddAsync(AuthSession session, CancellationToken ct = default);
    Task<AuthSession?> FindByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<AuthSession?> FindActiveByCurrentHashAsync(string tokenHash, CancellationToken ct = default);
    Task<AuthRefreshTokenHistory?> FindHistoryByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddHistoryAsync(AuthRefreshTokenHistory history, CancellationToken ct = default);
    Task RevokeFamilyAsync(Guid familyId, string reason, DateTimeOffset at, CancellationToken ct = default);
    Task RevokeSessionAsync(AuthSession session, string reason, DateTimeOffset at, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid userId, string reason, DateTimeOffset at, CancellationToken ct = default);

    /// <summary>
    /// Atomically consume expectedCurrentHash into history and set newHash. Returns false if lost a concurrent race.
    /// </summary>
    Task<bool> TryRotateCurrentHashAsync(
        Guid sessionId,
        string expectedCurrentHash,
        string newHash,
        Guid familyId,
        DateTimeOffset at,
        CancellationToken ct = default);
}
