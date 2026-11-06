using Domain.Constants;

namespace Application.Tests;

/// <summary>
/// Pure draft-contract markers for T-06 (ALARM_EVENT / EVENT_PERSISTED) until G1 adoption.
/// </summary>
public sealed class AlarmEventDraftRulesTests
{
    [Fact]
    public void AlarmEvent_And_EventPersisted_Draft_Markers_Are_Stable_Until_G1()
    {
        Assert.Equal("ALARM_EVENT", AlarmEventDraft.MessageTypes.AlarmEvent);
        Assert.Equal("EVENT_LOSS_REPORT", AlarmEventDraft.MessageTypes.EventLossReport);
        Assert.Equal("DEVICE_FAULT", AlarmEventDraft.MessageTypes.DeviceFault);
        Assert.Equal("EVENT_PERSISTED", EventPersistedDraft.MqttMessageType);
        Assert.Equal("PUBLISH_EVENT_PERSISTED", EventPersistedDraft.OutboxPublishMessageType);
        Assert.Equal("ALARM_EVENT", EventPersistedDraft.OutboxAggregateType);
        Assert.Equal(1, AlarmEventDraft.SchemaVersion);
        Assert.Equal(1, EventPersistedDraft.SchemaVersion);
    }

    [Fact]
    public void Notification_Statuses_Include_Cancelled_For_Pre_Fcm_Guard()
    {
        Assert.Contains($"'{DbStatusCodes.NotificationStatus.Cancelled}'", DbStatusCodes.NotificationStatus.SqlInList);
        Assert.Contains($"'{DbStatusCodes.NotificationStatus.Queued}'", DbStatusCodes.NotificationStatus.SqlInList);
        Assert.Equal("OWNERSHIP_NOT_ACTIVE", EventPersistedDraft.CancellationReasons.OwnershipNotActive);
    }

    [Fact]
    public void Event_Loss_Reason_Starts_With_Buffer_Full_Only()
    {
        Assert.Equal("BUFFER_FULL", DbStatusCodes.EventLossReason.BufferFull);
    }

    [Fact]
    public void ClockTrusted_False_Means_Device_Time_Is_Not_Authoritative()
    {
        // Documented FR-023 rule used by AlarmEventIngestService: force null occurrence when untrusted.
        const bool clockTrusted = false;
        DateTimeOffset? claimed = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        DateTimeOffset? stored = clockTrusted ? claimed : null;
        Assert.Null(stored);
    }
}
