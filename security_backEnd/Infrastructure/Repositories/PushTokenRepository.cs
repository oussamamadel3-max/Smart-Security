using Application.Interfaces.Repositories;
using Domain.Entities.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PushTokenRepository : IPushTokenRepository
{
    private readonly AppDbContext _db;

    public PushTokenRepository(AppDbContext db) => _db = db;

    public Task<PushToken?> FindByFingerprintAsync(byte[] fingerprint, CancellationToken ct = default)
        => _db.PushTokens.SingleOrDefaultAsync(t => t.TokenFingerprint == fingerprint, ct);

    public Task<PushToken?> GetByIdAsync(Guid pushTokenId, CancellationToken ct = default)
        => _db.PushTokens.SingleOrDefaultAsync(t => t.Id == pushTokenId, ct);

    public Task<PushToken?> GetByIdForUserAsync(Guid pushTokenId, Guid userId, CancellationToken ct = default)
        => _db.PushTokens.SingleOrDefaultAsync(t => t.Id == pushTokenId && t.UserId == userId, ct);

    public async Task<IReadOnlyList<PushToken>> ListByUserAsync(
        Guid userId,
        bool activeOnly,
        CancellationToken ct = default)
    {
        var q = _db.PushTokens.AsNoTracking().Where(t => t.UserId == userId);
        if (activeOnly)
            q = q.Where(t => t.RevokedAt == null);

        return await q.OrderByDescending(t => t.RegisteredAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PushToken>> ListActiveForUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.PushTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .OrderBy(t => t.RegisteredAt)
            .ToListAsync(ct);

    public void Add(PushToken token) => _db.PushTokens.Add(token);

    public async Task<int> RevokeAllActiveForUserAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        CancellationToken ct = default)
        => await _db.PushTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAt, revokedAt),
                ct);

    public async Task<int> RevokeActiveByIdsAsync(
        IReadOnlyCollection<Guid> pushTokenIds,
        DateTimeOffset revokedAt,
        CancellationToken ct = default)
    {
        if (pushTokenIds.Count == 0)
            return 0;

        return await _db.PushTokens
            .Where(t => pushTokenIds.Contains(t.Id) && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAt, revokedAt),
                ct);
    }
}
