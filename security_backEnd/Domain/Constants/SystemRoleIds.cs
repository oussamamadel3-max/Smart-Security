namespace Domain.Constants;

/// <summary>
/// Stable identifiers for system roles seeded on empty databases.
/// </summary>
public static class SystemRoleIds
{
    public static readonly Guid Admin = Guid.Parse("22222222-2222-2222-2222-222222222201");
    public static readonly Guid Owner = Guid.Parse("22222222-2222-2222-2222-222222222202");
}
