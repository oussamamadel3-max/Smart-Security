using Domain.Entities.Notifications;

namespace Application.Interfaces.Repositories;

public interface IPushTokenRepository
{
    Task<PushToken?> FindByFingerprintAsync(byte[] fingerprint, CancellationToken ct = default);

    Task<PushToken?> GetByIdAsync(Guid pushTokenId, CancellationToken ct = default);

    Task<PushToken?> GetByIdForUserAsync(Guid pushTokenId, Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<PushToken>> ListByUserAsync(Guid userId, bool activeOnly, CancellationToken ct = default);

    Task<IReadOnlyList<PushToken>> ListActiveForUserAsync(Guid userId, CancellationToken ct = default);

    void Add(PushToken token);

    Task<int> RevokeAllActiveForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken ct = default);

    Task<int> RevokeActiveByIdsAsync(
        IReadOnlyCollection<Guid> pushTokenIds,
        DateTimeOffset revokedAt,
        CancellationToken ct = default);
}
