namespace Application.Dtos.AdminDtos;

public sealed class CreateOwnerRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public sealed class UpdateOwnerRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public sealed class OwnerAccountResponseDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
