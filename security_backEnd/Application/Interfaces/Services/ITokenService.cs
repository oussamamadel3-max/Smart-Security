using Domain.Entities.Auth;

namespace Application.Interfaces.Services;

// User entity used for access-token claims.

public interface ITokenService
{
    string CreateAccessToken(User user, IEnumerable<string> roles, Guid sessionId, out DateTimeOffset expiresAt);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
