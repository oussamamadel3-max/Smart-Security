using Domain.Constants;

namespace Application.Common.Devices;

/// <summary>
/// Pure ownership transfer / unlink gates (BE-T-07 / BE-T-10).
/// </summary>
public static class OwnershipTransferAcceptanceRules
{
    public static string CloseStatusForOpenOwnership(string openStatus)
        => openStatus == DbStatusCodes.Ownership.Pending
            ? DbStatusCodes.Ownership.Cancelled
            : DbStatusCodes.Ownership.Ended;

    /// <summary>
    /// New ACTIVE ownership may accept a command request when online, but Outbox must hold
    /// until the device acknowledges the new ownership version.
    /// </summary>
    public static bool MayAcceptCommandRequestBeforeOwnershipAck(
        string newOwnershipStatus,
        bool deviceOnline)
        => newOwnershipStatus == DbStatusCodes.Ownership.Active && deviceOnline;

    public static bool MayPublishCommandBeforeOwnershipAck(bool ownershipVersionAcknowledgedByDevice)
        => ownershipVersionAcknowledgedByDevice;

    public static bool PriorOwnerMayReadDeviceHistoryAfterTransfer() => false;
}
