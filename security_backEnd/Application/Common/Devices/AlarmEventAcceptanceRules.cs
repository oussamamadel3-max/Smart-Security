using Domain.Constants;

namespace Application.Common.Devices;

/// <summary>
/// Pure ALARM_EVENT / notification acceptance rules (BE-T-06 / BE-T-10).
/// </summary>
public static class AlarmEventAcceptanceRules
{
    public static DateTimeOffset? ResolveOccurrenceAt(bool clockTrusted, DateTimeOffset? claimedOccurrenceAt)
        => clockTrusted ? claimedOccurrenceAt : null;

    public static bool ShouldCreateOwnerNotification(string ownershipStatus)
        => string.Equals(ownershipStatus, DbStatusCodes.Ownership.Active, StringComparison.Ordinal);

    /// <summary>
    /// Same (deviceId, eventUid) redelivery must not invent a second notification request.
    /// </summary>
    public static bool IsDuplicateEvent(bool alreadyPersistedForDeviceEventUid)
        => alreadyPersistedForDeviceEventUid;

    public static bool KeepAdminAlertWhenOwnershipEnds() => true;

    public static bool CancelPendingAlarmNotificationOnOwnershipEnd() => true;
}
