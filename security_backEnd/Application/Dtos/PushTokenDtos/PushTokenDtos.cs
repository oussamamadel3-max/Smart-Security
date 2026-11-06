using System.ComponentModel.DataAnnotations;

namespace Application.Dtos.PushTokenDtos;

public sealed class RegisterPushTokenRequestDto
{
    /// <summary>Raw FCM registration token from the client SDK. Never logged.</summary>
    [Required]
    [MinLength(16)]
    [MaxLength(4096)]
    public string Token { get; set; } = string.Empty;

    /// <summary>ANDROID | IOS | WEB</summary>
    [Required]
    [MaxLength(12)]
    public string Platform { get; set; } = string.Empty;
}

public sealed class PushTokenResponseDto
{
    public Guid PushTokenId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class PushTokenListItemDto
{
    public Guid PushTokenId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
