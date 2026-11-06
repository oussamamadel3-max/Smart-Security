namespace Application.Dtos.AuthDtos;

public sealed class RefreshRequestDto
{
    /// <summary>Raw refresh token when using BODY channel. Must be absent when Cookie is used.</summary>
    public string? RefreshToken { get; set; }
}
