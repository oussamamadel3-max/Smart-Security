using Application.Common.Devices;
using Domain.Constants;

namespace Application.Tests;

/// <summary>
/// SS-P9-010: pure unit coverage for ownership/command/conflict/timeout/idempotency gates.
/// Complements integration tests; does not claim MQTT live execution.
/// </summary>
public sealed class OwnerDeviceCommandAcceptanceRulesTests
{
    [Theory]
    [InlineData(DbStatusCodes.CommandType.Arm, true)]
    [InlineData(DbStatusCodes.CommandType.Disarm, true)]
    [InlineData(DbStatusCodes.CommandType.MuteAlarm, true)]
    [InlineData(DbStatusCodes.CommandType.TestSiren, true)]
    [InlineData(DbStatusCodes.CommandType.StopTest, true)]
    [InlineData("REBOOT", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Supported_Command_Types_Are_Explicit(string? type, bool expected)
        => Assert.Equal(expected, OwnerDeviceCommandAcceptanceRules.IsSupportedCommandType(type));

    [Theory]
    [InlineData(DbStatusCodes.Ownership.Active, true)]
    [InlineData(DbStatusCodes.Ownership.Pending, false)]
    [InlineData(DbStatusCodes.Ownership.Ended, false)]
    [InlineData(DbStatusCodes.Ownership.Cancelled, false)]
    public void Only_Active_Ownership_Is_Controllable(string status, bool expected)
        => Assert.Equal(expected, OwnerDeviceCommandAcceptanceRules.IsOwnershipControllable(status));

    [Fact]
    public void Pending_Blocks_Conflicting_Commands()
        => Assert.True(OwnerDeviceCommandAcceptanceRules.BlocksConflictingCommand(
            DbStatusCodes.CommandStatus.Pending,
            reconciledAt: null));

    [Fact]
    public void TimedOut_Without_Reconciliation_Blocks_Conflict()
        => Assert.True(OwnerDeviceCommandAcceptanceRules.BlocksConflictingCommand(
            DbStatusCodes.CommandStatus.TimedOut,
            reconciledAt: null));

    [Fact]
    public void TimedOut_After_Reconciliation_Does_Not_Block()
        => Assert.False(OwnerDeviceCommandAcceptanceRules.BlocksConflictingCommand(
            DbStatusCodes.CommandStatus.TimedOut,
            reconciledAt: DateTimeOffset.Parse("2026-11-06T12:00:00Z")));

    [Fact]
    public void Executed_Does_Not_Block_New_Command()
        => Assert.False(OwnerDeviceCommandAcceptanceRules.BlocksConflictingCommand(
            DbStatusCodes.CommandStatus.Executed,
            reconciledAt: null));

    [Fact]
    public void Ack_Timeout_Uses_SentAt_When_Present()
    {
        var created = DateTimeOffset.Parse("2026-11-06T12:00:00Z");
        var sent = DateTimeOffset.Parse("2026-11-06T12:00:05Z");
        var now = DateTimeOffset.Parse("2026-11-06T12:00:14Z");
        Assert.False(OwnerDeviceCommandAcceptanceRules.IsAckTimeoutDue(created, sent, 10, now));
        Assert.True(OwnerDeviceCommandAcceptanceRules.IsAckTimeoutDue(created, sent, 10, now.AddSeconds(1)));
    }

    [Fact]
    public void Ack_Timeout_Falls_Back_To_CreatedAt_When_Not_Sent()
    {
        var created = DateTimeOffset.Parse("2026-11-06T12:00:00Z");
        Assert.True(OwnerDeviceCommandAcceptanceRules.IsAckTimeoutDue(
            created,
            sentAt: null,
            commandTimeoutSeconds: 10,
            utcNow: created.AddSeconds(10)));
    }

    [Fact]
    public void Mute_Requires_Matching_Current_Alarm_Uid()
    {
        var uid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Assert.Equal(
            ExecuteCommandDraft.RejectCodes.MuteTargetRequired,
            OwnerDeviceCommandAcceptanceRules.EvaluateMuteRule(null, uid));
        Assert.Equal(
            ExecuteCommandDraft.RejectCodes.MuteTargetMismatch,
            OwnerDeviceCommandAcceptanceRules.EvaluateMuteRule(Guid.NewGuid(), uid));
        Assert.Null(OwnerDeviceCommandAcceptanceRules.EvaluateMuteRule(uid, uid));
    }

    [Fact]
    public void TestSiren_Rejected_During_Alarm_StopTest_Requires_Test()
    {
        Assert.Equal(
            ExecuteCommandDraft.RejectCodes.TestDuringAlarm,
            OwnerDeviceCommandAcceptanceRules.EvaluateTestSirenRule(
                DbStatusCodes.Siren.Alarm,
                currentAlarmEventUid: Guid.NewGuid()));
        Assert.Null(OwnerDeviceCommandAcceptanceRules.EvaluateTestSirenRule(
            DbStatusCodes.Siren.Off,
            currentAlarmEventUid: null));
        Assert.Equal(
            ExecuteCommandDraft.RejectCodes.StopTestNotActive,
            OwnerDeviceCommandAcceptanceRules.EvaluateStopTestRule(DbStatusCodes.Siren.Off));
        Assert.Null(OwnerDeviceCommandAcceptanceRules.EvaluateStopTestRule(DbStatusCodes.Siren.Test));
    }

    [Fact]
    public void Idempotent_Replay_Requires_Same_Owner_Device_Type_And_Mute_Target()
    {
        var owner = Guid.NewGuid();
        var device = Guid.NewGuid();
        var alarm = Guid.NewGuid();

        Assert.True(OwnerDeviceCommandAcceptanceRules.MatchesIdempotentReplay(
            owner, owner, device, device,
            DbStatusCodes.CommandType.Arm, DbStatusCodes.CommandType.Arm,
            null, null));

        Assert.False(OwnerDeviceCommandAcceptanceRules.MatchesIdempotentReplay(
            owner, owner, device, device,
            DbStatusCodes.CommandType.Arm, DbStatusCodes.CommandType.Disarm,
            null, null));

        Assert.True(OwnerDeviceCommandAcceptanceRules.MatchesIdempotentReplay(
            owner, owner, device, device,
            DbStatusCodes.CommandType.MuteAlarm, DbStatusCodes.CommandType.MuteAlarm,
            alarm, alarm));

        Assert.False(OwnerDeviceCommandAcceptanceRules.MatchesIdempotentReplay(
            owner, Guid.NewGuid(), device, device,
            DbStatusCodes.CommandType.Arm, DbStatusCodes.CommandType.Arm,
            null, null));
    }

    [Fact]
    public void Http_Acceptance_Never_Changes_Confirmed_Arm_State()
        => Assert.False(OwnerDeviceCommandAcceptanceRules.HttpAcceptanceChangesConfirmedArmState());
}
