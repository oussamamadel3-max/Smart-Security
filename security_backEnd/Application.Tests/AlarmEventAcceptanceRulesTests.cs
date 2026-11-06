using Application.Common.Devices;
using Domain.Constants;

namespace Application.Tests;

/// <summary>
/// SS-P9-010: ALARM_EVENT redelivery / notification / clock rules (AC-T-04 / AC-T-23 / AC-T-24).
/// </summary>
public sealed class AlarmEventAcceptanceRulesTests
{
    [Fact]
    public void Untrusted_Clock_Clears_Occurrence_At()
    {
        var claimed = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        Assert.Null(AlarmEventAcceptanceRules.ResolveOccurrenceAt(clockTrusted: false, claimed));
        Assert.Equal(claimed, AlarmEventAcceptanceRules.ResolveOccurrenceAt(clockTrusted: true, claimed));
    }

    [Theory]
    [InlineData(DbStatusCodes.Ownership.Active, true)]
    [InlineData(DbStatusCodes.Ownership.Pending, false)]
    [InlineData(DbStatusCodes.Ownership.Ended, false)]
    public void Notification_Only_For_Active_Ownership(string status, bool expected)
        => Assert.Equal(expected, AlarmEventAcceptanceRules.ShouldCreateOwnerNotification(status));

    [Fact]
    public void Duplicate_Event_Is_Detected_For_Same_Device_EventUid()
        => Assert.True(AlarmEventAcceptanceRules.IsDuplicateEvent(alreadyPersistedForDeviceEventUid: true));

    [Fact]
    public void Ownership_End_Cancels_Alarm_Notification_But_Keeps_Admin_Alert()
    {
        Assert.True(AlarmEventAcceptanceRules.CancelPendingAlarmNotificationOnOwnershipEnd());
        Assert.True(AlarmEventAcceptanceRules.KeepAdminAlertWhenOwnershipEnds());
    }
}
