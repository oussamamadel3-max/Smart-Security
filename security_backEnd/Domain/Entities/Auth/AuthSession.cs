namespace Domain.Entities.Auth;

/// <summary>
/// Server-side auth session bound to a refresh-token family (RFC 9700 style rotation).
/// </summary>
public class AuthSession
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// How refresh is delivered for this session: COOKIE (web) or BODY (mobile).
    /// Chosen from transport rules at login — not a client self-assertion of app identity.
    /// </summary>
    public string RefreshChannel { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the currently valid refresh token only (never the raw token).
    /// </summary>
    public string CurrentRefreshTokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AbsoluteExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }

    public ICollection<AuthRefreshTokenHistory> TokenHistory { get; set; } = new List<AuthRefreshTokenHistory>();
}
