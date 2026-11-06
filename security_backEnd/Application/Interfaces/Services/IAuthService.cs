using Application.Dtos.AuthDtos;

namespace Application.Interfaces.Services;

/// <summary>
/// Authentication and session operations.
/// </summary>
public interface IAuthService
{
    Task<MeResponseDto> GetMeAsync(Guid userId, CancellationToken ct = default);

    Task<AuthTokensResponseDto> LoginAsync(
        LoginRequestDto request,
        string refreshChannel,
        CancellationToken ct = default);

    Task<AuthTokensResponseDto> RefreshAsync(
        string rawRefreshToken,
        string refreshChannel,
        CancellationToken ct = default);

    Task LogoutAsync(Guid userId, Guid sessionId, string? rawRefreshToken, CancellationToken ct = default);

    Task LogoutAllAsync(Guid userId, CancellationToken ct = default);

    Task EnsureUserAndSessionActiveAsync(Guid userId, Guid sessionId, CancellationToken ct = default);

    /// <summary>Resolves active session id from a raw refresh token (hash lookup). Null if unknown/revoked.</summary>
    Task<Guid?> FindActiveSessionIdByRefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default);
}
