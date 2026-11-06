namespace Domain.Entities.Auth;

/// <summary>
/// Consumed refresh-token hashes for a session/family — used for replay detection.
/// </summary>
public class AuthRefreshTokenHistory
{
    public long Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid SessionId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ConsumedAt { get; set; }
}
