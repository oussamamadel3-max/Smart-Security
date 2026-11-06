using Application.Common.Devices;
using Domain.Constants;

namespace Application.Tests;

/// <summary>
/// SS-P9-010: ownership transfer / publish-hold rules (AC-T-10 / AC-T-24).
/// Extends marker-only OwnershipTransferRulesTests with behavioural gates.
/// </summary>
public sealed class OwnershipTransferAcceptanceRulesTests
{
    [Theory]
    [InlineData(DbStatusCodes.Ownership.Pending, DbStatusCodes.Ownership.Cancelled)]
    [InlineData(DbStatusCodes.Ownership.Active, DbStatusCodes.Ownership.Ended)]
    public void Close_Status_Depends_On_Open_Status(string open, string expected)
        => Assert.Equal(expected, OwnershipTransferAcceptanceRules.CloseStatusForOpenOwnership(open));

    [Fact]
    public void Online_New_Owner_May_Accept_Request_But_Not_Publish_Before_Ack()
    {
        Assert.True(OwnershipTransferAcceptanceRules.MayAcceptCommandRequestBeforeOwnershipAck(
            DbStatusCodes.Ownership.Active,
            deviceOnline: true));
        Assert.False(OwnershipTransferAcceptanceRules.MayPublishCommandBeforeOwnershipAck(
            ownershipVersionAcknowledgedByDevice: false));
        Assert.True(OwnershipTransferAcceptanceRules.MayPublishCommandBeforeOwnershipAck(
            ownershipVersionAcknowledgedByDevice: true));
    }

    [Fact]
    public void Offline_New_Owner_Cannot_Accept_Instant_Control()
        => Assert.False(OwnershipTransferAcceptanceRules.MayAcceptCommandRequestBeforeOwnershipAck(
            DbStatusCodes.Ownership.Active,
            deviceOnline: false));

    [Fact]
    public void Prior_Owner_History_Does_Not_Leak_After_Transfer()
        => Assert.False(OwnershipTransferAcceptanceRules.PriorOwnerMayReadDeviceHistoryAfterTransfer());
}
