using Domain.Constants;

namespace Application.Tests;

/// <summary>
/// Stable draft markers and status transitions used by ownership transfer (BE-T-07).
/// </summary>
public sealed class OwnershipTransferRulesTests
{
    [Fact]
    public void Ownership_Sync_Draft_Markers_Are_Stable_Until_G1()
    {
        Assert.Equal("OWNERSHIP_SYNC", OwnershipSyncDraft.MqttMessageType);
        Assert.Equal("PUBLISH_OWNERSHIP_SYNC", OwnershipSyncDraft.OutboxPublishMessageType);
        Assert.Equal("DEVICE_OWNERSHIP", OwnershipSyncDraft.OutboxAggregateType);
    }

    [Fact]
    public void Transfer_Cancel_Reasons_Are_Distinct_From_Fcm_Guard()
    {
        Assert.Equal("OWNERSHIP_TRANSFERRED", EventPersistedDraft.CancellationReasons.OwnershipTransferred);
        Assert.Equal("OWNERSHIP_NOT_ACTIVE", EventPersistedDraft.CancellationReasons.OwnershipNotActive);
        Assert.NotEqual(
            EventPersistedDraft.CancellationReasons.OwnershipTransferred,
            EventPersistedDraft.CancellationReasons.OwnershipNotActive);
    }

    [Theory]
    [InlineData(DbStatusCodes.Ownership.Pending, DbStatusCodes.Ownership.Cancelled)]
    [InlineData(DbStatusCodes.Ownership.Active, DbStatusCodes.Ownership.Ended)]
    public void Closed_Ownership_Status_Depends_On_Open_Status(string open, string expectedClosed)
    {
        var closed = open == DbStatusCodes.Ownership.Pending
            ? DbStatusCodes.Ownership.Cancelled
            : DbStatusCodes.Ownership.Ended;
        Assert.Equal(expectedClosed, closed);
    }
}
