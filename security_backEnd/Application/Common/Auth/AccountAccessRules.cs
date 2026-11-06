namespace Application.Common.Auth;

/// <summary>
/// Account usability gates shared by auth refresh and protected operations (FR-001 / AC-T-20).
/// </summary>
public static class AccountAccessRules
{
    public static bool IsUsable(bool isActive, DateTimeOffset? deletedAt)
        => isActive && deletedAt is null;

    public static bool MustRejectProtectedOperation(bool isActive, DateTimeOffset? deletedAt)
        => !IsUsable(isActive, deletedAt);
}
