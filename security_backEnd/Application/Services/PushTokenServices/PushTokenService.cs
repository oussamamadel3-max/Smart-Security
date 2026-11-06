using Application.Dtos.PushTokenDtos;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Notifications;
using Domain.Exceptions;

namespace Application.Services.PushTokenServices;

public sealed class PushTokenService : IPushTokenService
{
    private readonly IPushTokenRepository _tokens;
    private readonly IPushTokenCrypto _crypto;
    private readonly IUnitOfWork _uow;

    public PushTokenService(
        IPushTokenRepository tokens,
        IPushTokenCrypto crypto,
        IUnitOfWork uow)
    {
        _tokens = tokens;
        _crypto = crypto;
        _uow = uow;
    }

    public async Task<PushTokenResponseDto> RegisterOrUpdateAsync(
        Guid userId,
        RegisterPushTokenRequestDto request,
        CancellationToken ct = default)
    {
        var platform = NormalizePlatform(request.Platform);
        var raw = request.Token.Trim();
        if (raw.Length < 16)
            throw new BadRequestException("رمز الدفع غير صالح.", "PUSH_TOKEN_INVALID");

        var fingerprint = _crypto.ComputeFingerprint(raw);
        var ciphertext = _crypto.Encrypt(raw);
        var now = DateTimeOffset.UtcNow;

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var existing = await _tokens.FindByFingerprintAsync(fingerprint, ct);
            if (existing is null)
            {
                var created = new PushToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TokenCiphertext = ciphertext,
                    TokenFingerprint = fingerprint,
                    Platform = platform,
                    RegisteredAt = now,
                    LastUsedAt = null,
                    RevokedAt = null
                };
                _tokens.Add(created);
                await _uow.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return MapResponse(created);
            }

            // Same fingerprint under another account: revoke prior ownership, reassign atomically.
            if (existing.UserId != userId)
            {
                if (existing.RevokedAt is null)
                    existing.RevokedAt = now;

                var reassigned = new PushToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TokenCiphertext = ciphertext,
                    TokenFingerprint = fingerprint,
                    Platform = platform,
                    RegisteredAt = now,
                    LastUsedAt = null,
                    RevokedAt = null
                };

                // UNIQUE(token_fingerprint) requires clearing the old fingerprint first.
                existing.TokenFingerprint = DeriveRetiredFingerprint(existing.Id, fingerprint);
                existing.TokenCiphertext = Array.Empty<byte>();
                await _uow.SaveChangesAsync(ct);

                _tokens.Add(reassigned);
                await _uow.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return MapResponse(reassigned);
            }

            existing.TokenCiphertext = ciphertext;
            existing.Platform = platform;
            existing.RevokedAt = null;
            if (existing.RegisteredAt == default)
                existing.RegisteredAt = now;

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return MapResponse(existing);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<PushTokenListItemDto>> ListMineAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var rows = await _tokens.ListByUserAsync(userId, activeOnly: false, ct);
        return rows.Select(t => new PushTokenListItemDto
        {
            PushTokenId = t.Id,
            Platform = t.Platform,
            RegisteredAt = t.RegisteredAt,
            LastUsedAt = t.LastUsedAt,
            RevokedAt = t.RevokedAt
        }).ToList();
    }

    public async Task RevokeAsync(
        Guid userId,
        Guid pushTokenId,
        CancellationToken ct = default)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var token = await _tokens.GetByIdForUserAsync(pushTokenId, userId, ct)
                ?? throw new NotFoundException("رمز الدفع غير موجود.", "PUSH_TOKEN_NOT_FOUND");

            if (token.RevokedAt is null)
                token.RevokedAt = DateTimeOffset.UtcNow;

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task RevokeAllActiveAsync(Guid userId, CancellationToken ct = default)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            await _tokens.RevokeAllActiveForUserAsync(userId, DateTimeOffset.UtcNow, ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static string NormalizePlatform(string platform)
    {
        var p = platform.Trim().ToUpperInvariant();
        return p switch
        {
            DbStatusCodes.PushPlatform.Android => DbStatusCodes.PushPlatform.Android,
            DbStatusCodes.PushPlatform.Ios => DbStatusCodes.PushPlatform.Ios,
            DbStatusCodes.PushPlatform.Web => DbStatusCodes.PushPlatform.Web,
            _ => throw new BadRequestException("منصة رمز الدفع غير مدعومة.", "PUSH_PLATFORM_INVALID")
        };
    }

    /// <summary>
    /// Preserves uniqueness after account reassignment without storing the live fingerprint twice.
    /// </summary>
    private static byte[] DeriveRetiredFingerprint(Guid retiredTokenId, byte[] original)
    {
        var prefix = retiredTokenId.ToByteArray();
        var retired = new byte[prefix.Length + original.Length];
        Buffer.BlockCopy(prefix, 0, retired, 0, prefix.Length);
        Buffer.BlockCopy(original, 0, retired, prefix.Length, original.Length);
        // Shrink to 32 bytes via SHA-256 so the column stays fingerprint-sized.
        return System.Security.Cryptography.SHA256.HashData(retired);
    }

    private static PushTokenResponseDto MapResponse(PushToken token)
        => new()
        {
            PushTokenId = token.Id,
            Platform = token.Platform,
            RegisteredAt = token.RegisteredAt,
            LastUsedAt = token.LastUsedAt,
            IsActive = token.RevokedAt is null
        };
}
