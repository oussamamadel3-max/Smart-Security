using Application.Interfaces.Repositories;
using Domain.Entities.Auth;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AuthSessionRepository : IAuthSessionRepository
{
    private readonly AppDbContext _db;

    public AuthSessionRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(AuthSession session, CancellationToken ct = default)
    {
        _db.AuthSessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<AuthSession?> FindByIdAsync(Guid sessionId, CancellationToken ct = default)
        => _db.AuthSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public Task<AuthSession?> FindActiveByCurrentHashAsync(string tokenHash, CancellationToken ct = default)
        => _db.AuthSessions.FirstOrDefaultAsync(
            s => s.CurrentRefreshTokenHash == tokenHash && s.RevokedAt == null,
            ct);

    public Task<AuthRefreshTokenHistory?> FindHistoryByHashAsync(string tokenHash, CancellationToken ct = default)
        => _db.AuthRefreshTokenHistory.AsNoTracking()
            .FirstOrDefaultAsync(h => h.TokenHash == tokenHash, ct);

    public Task AddHistoryAsync(AuthRefreshTokenHistory history, CancellationToken ct = default)
    {
        _db.AuthRefreshTokenHistory.Add(history);
        return Task.CompletedTask;
    }

    public async Task RevokeFamilyAsync(Guid familyId, string reason, DateTimeOffset at, CancellationToken ct = default)
    {
        await _db.AuthSessions
            .Where(s => s.FamilyId == familyId && s.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(x => x.RevokedAt, at)
                    .SetProperty(x => x.RevokeReason, reason),
                ct);
    }

    public Task RevokeSessionAsync(AuthSession session, string reason, DateTimeOffset at, CancellationToken ct = default)
    {
        session.RevokedAt = at;
        session.RevokeReason = reason;
        return Task.CompletedTask;
    }

    public async Task RevokeAllForUserAsync(Guid userId, string reason, DateTimeOffset at, CancellationToken ct = default)
    {
        await _db.AuthSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(x => x.RevokedAt, at)
                    .SetProperty(x => x.RevokeReason, reason),
                ct);
    }

    public async Task<bool> TryRotateCurrentHashAsync(
        Guid sessionId,
        string expectedCurrentHash,
        string newHash,
        Guid familyId,
        DateTimeOffset at,
        CancellationToken ct = default)
    {
        var affected = await _db.AuthSessions
            .Where(s =>
                s.Id == sessionId &&
                s.CurrentRefreshTokenHash == expectedCurrentHash &&
                s.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(x => x.CurrentRefreshTokenHash, newHash)
                    .SetProperty(x => x.LastUsedAt, at),
                ct);

        if (affected != 1)
            return false;

        _db.AuthRefreshTokenHistory.Add(new AuthRefreshTokenHistory
        {
            FamilyId = familyId,
            SessionId = sessionId,
            TokenHash = expectedCurrentHash,
            ConsumedAt = at
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
