namespace Application.Dtos.AuthDtos;

/// <summary>
/// Current authenticated user profile payload.
/// </summary>
public sealed class MeResponseDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
}
