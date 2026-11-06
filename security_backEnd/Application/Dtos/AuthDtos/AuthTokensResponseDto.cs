using System.Text.Json.Serialization;

namespace Application.Dtos.AuthDtos;

public sealed class AuthTokensResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
    public MeResponseDto User { get; set; } = new();
    public string RefreshChannel { get; set; } = string.Empty;

    /// <summary>Server-side only — used to bind signed CSRF; never serialized to clients.</summary>
    [JsonIgnore]
    public Guid SessionId { get; set; }
}
