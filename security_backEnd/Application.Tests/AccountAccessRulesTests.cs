using Application.Common.Auth;

namespace Application.Tests;

/// <summary>
/// SS-P9-010: disabled / archived account rejection rules (AC-T-20).
/// </summary>
public sealed class AccountAccessRulesTests
{
    [Fact]
    public void Active_Non_Deleted_Account_Is_Usable()
        => Assert.True(AccountAccessRules.IsUsable(isActive: true, deletedAt: null));

    [Fact]
    public void Disabled_Account_Must_Reject_Protected_Operations()
    {
        Assert.True(AccountAccessRules.MustRejectProtectedOperation(isActive: false, deletedAt: null));
        Assert.False(AccountAccessRules.IsUsable(isActive: false, deletedAt: null));
    }

    [Fact]
    public void Soft_Deleted_Account_Must_Reject_Even_If_Flag_Still_Active()
    {
        var deletedAt = DateTimeOffset.Parse("2026-11-06T12:00:00Z");
        Assert.True(AccountAccessRules.MustRejectProtectedOperation(isActive: true, deletedAt: deletedAt));
    }
}
