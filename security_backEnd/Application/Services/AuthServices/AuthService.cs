using Application.Common.Auth;
using Application.Dtos.AuthDtos;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Options;
using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Exceptions;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Application.Services.AuthServices;

/// <summary>
/// Login / refresh rotation / logout with server-side session validation.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<User> _users;
    private readonly IAuthSessionRepository _sessions;
    private readonly ITokenService _tokens;
    private readonly IUnitOfWork _uow;
    private readonly AuthOptions _options;

    public AuthService(
        UserManager<User> users,
        IAuthSessionRepository sessions,
        ITokenService tokens,
        IUnitOfWork uow,
        IOptions<AuthOptions> options)
    {
        _users = users;
        _sessions = sessions;
        _tokens = tokens;
        _uow = uow;
        _options = options.Value;
    }

    public async Task<MeResponseDto> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await RequireActiveUserAsync(userId, ct);
        return user.Adapt<MeResponseDto>();
    }

    public async Task EnsureUserAndSessionActiveAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        await RequireActiveUserAsync(userId, ct);
        var session = await _sessions.FindByIdAsync(sessionId, ct)
            ?? throw new UnauthenticatedUserException();

        if (session.UserId != userId ||
            session.RevokedAt is not null ||
            session.AbsoluteExpiresAt <= DateTimeOffset.UtcNow)
            throw new UnauthenticatedUserException();
    }

    public async Task<AuthTokensResponseDto> LoginAsync(
        LoginRequestDto request,
        string refreshChannel,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            throw new UnauthenticatedUserException("فشل تسجيل الدخول.");

        if (refreshChannel is not (AuthStatusCodes.RefreshChannel.Cookie or AuthStatusCodes.RefreshChannel.Body))
            throw new BadRequestException("قناة التحديث غير مدعومة.", "AUTH_CHANNEL_INVALID");

        var user = await _users.FindByNameAsync(request.UserName.Trim());
        if (user is null || !await _users.CheckPasswordAsync(user, request.Password))
            throw new UnauthenticatedUserException("فشل تسجيل الدخول.");

        EnsureAccountUsable(user);

        var roles = await _users.GetRolesAsync(user);
        var now = DateTimeOffset.UtcNow;
        var rawRefresh = _tokens.GenerateRefreshToken();
        var hash = _tokens.HashRefreshToken(rawRefresh);

        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            UserId = user.Id,
            RefreshChannel = refreshChannel,
            CurrentRefreshTokenHash = hash,
            CreatedAt = now,
            AbsoluteExpiresAt = now.AddDays(_options.RefreshTokenDays),
            LastUsedAt = now
        };

        await _sessions.AddAsync(session, ct);
        await _uow.SaveChangesAsync(ct);

        // Always return raw refresh to the API layer; Cookie channel clears it after Set-Cookie.
        return BuildTokenResponse(user, roles, session, rawRefresh);
    }

    public async Task<AuthTokensResponseDto> RefreshAsync(
        string rawRefreshToken,
        string refreshChannel,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            throw new UnauthenticatedUserException();

        var presentedHash = _tokens.HashRefreshToken(rawRefreshToken);
        var now = DateTimeOffset.UtcNow;

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var history = await _sessions.FindHistoryByHashAsync(presentedHash, ct);
            if (history is not null)
            {
                // RFC 9700: confirmed reuse → revoke the affected family only.
                await _sessions.RevokeFamilyAsync(history.FamilyId, AuthStatusCodes.RevokeReason.RefreshReuse, now, ct);
                await _uow.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                throw new UnauthenticatedUserException();
            }

            var session = await _sessions.FindActiveByCurrentHashAsync(presentedHash, ct);
            if (session is null)
                throw new UnauthenticatedUserException();

            if (!string.Equals(session.RefreshChannel, refreshChannel, StringComparison.Ordinal))
                throw new UnauthenticatedUserException();

            if (session.AbsoluteExpiresAt <= now)
            {
                await _sessions.RevokeSessionAsync(session, AuthStatusCodes.RevokeReason.AbsoluteExpired, now, ct);
                await _uow.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                throw new UnauthenticatedUserException();
            }

            var user = await _users.FindByIdAsync(session.UserId.ToString())
                ?? throw new UnauthenticatedUserException();
            EnsureAccountUsable(user);

            var expectedHash = session.CurrentRefreshTokenHash;
            var newRaw = _tokens.GenerateRefreshToken();
            var newHash = _tokens.HashRefreshToken(newRaw);

            // Atomic consume of current hash into history; no grace window for the consumed token.
            var rotated = await _sessions.TryRotateCurrentHashAsync(
                session.Id,
                expectedHash,
                newHash,
                session.FamilyId,
                now,
                ct);

            if (!rotated)
            {
                await tx.RollbackAsync(ct);
                throw new UnauthenticatedUserException();
            }

            await tx.CommitAsync(ct);

            session.CurrentRefreshTokenHash = newHash;
            session.LastUsedAt = now;

            var roles = await _users.GetRolesAsync(user);
            return BuildTokenResponse(user, roles, session, newRaw);
        }
        catch (UnauthenticatedUserException)
        {
            throw;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task LogoutAsync(Guid userId, Guid sessionId, string? rawRefreshToken, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        AuthSession? session = null;

        if (sessionId != Guid.Empty)
            session = await _sessions.FindByIdAsync(sessionId, ct);

        if (session is null && !string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            var hash = _tokens.HashRefreshToken(rawRefreshToken);
            session = await _sessions.FindActiveByCurrentHashAsync(hash, ct);
        }

        if (session is null || session.UserId != userId)
            return;

        await _sessions.RevokeSessionAsync(session, AuthStatusCodes.RevokeReason.Logout, now, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task LogoutAllAsync(Guid userId, CancellationToken ct = default)
    {
        await _sessions.RevokeAllForUserAsync(userId, AuthStatusCodes.RevokeReason.LogoutAll, DateTimeOffset.UtcNow, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<Guid?> FindActiveSessionIdByRefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return null;

        var hash = _tokens.HashRefreshToken(rawRefreshToken);
        var session = await _sessions.FindActiveByCurrentHashAsync(hash, ct);
        return session?.Id;
    }

    private AuthTokensResponseDto BuildTokenResponse(
        User user,
        IList<string> roles,
        AuthSession session,
        string rawRefresh)
    {
        var access = _tokens.CreateAccessToken(user, roles, session.Id, out var exp);
        return new AuthTokensResponseDto
        {
            AccessToken = access,
            TokenType = "Bearer",
            ExpiresIn = Math.Max(1, (int)(exp - DateTimeOffset.UtcNow).TotalSeconds),
            RefreshToken = rawRefresh,
            RefreshChannel = session.RefreshChannel,
            SessionId = session.Id,
            User = user.Adapt<MeResponseDto>()
        };
    }

    private async Task<User> RequireActiveUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(userId.ToString())
            ?? throw new UnauthenticatedUserException();
        EnsureAccountUsable(user);
        return user;
    }

    private static void EnsureAccountUsable(User user)
    {
        if (AccountAccessRules.MustRejectProtectedOperation(user.IsActive, user.DeletedAt))
            throw new UnauthenticatedUserException();
    }
}
