namespace Domain.Constants;

/// <summary>
/// Delivery outcome exposed by admin-alert APIs.
/// Distinct from <see cref="DbStatusCodes.NotificationStatus"/> so QUEUED without an FCM worker
/// is reported as PENDING (not inventing ACCEPTED).
/// FAILED/ACCEPTED require evidence of an actual provider attempt (<c>last_attempt_at</c>).
/// </summary>
public static class AdminAlertDeliveryCodes
{
    /// <summary>No completed provider attempt yet (includes QUEUED / SENDING before T-09).</summary>
    public const string Pending = "PENDING";

    /// <summary>Provider attempt completed with permanent failure.</summary>
    public const string Failed = "FAILED";

    /// <summary>Provider accepted at least one delivery attempt (real FCM only).</summary>
    public const string Accepted = "ACCEPTED";

    /// <summary>Notification cancelled (never used for ownership transfer of ADMIN_ALERT).</summary>
    public const string Cancelled = "CANCELLED";

    /// <summary>
    /// Maps stored notification status to API delivery outcome.
    /// Without <paramref name="lastAttemptAt"/>, FAILED/ACCEPTED collapse to PENDING
    /// so the API never claims a send result without an attempt timestamp.
    /// </summary>
    public static string FromNotificationStatus(
        string? notificationStatus,
        DateTimeOffset? lastAttemptAt = null)
        => notificationStatus switch
        {
            DbStatusCodes.NotificationStatus.Accepted when lastAttemptAt is not null => Accepted,
            DbStatusCodes.NotificationStatus.Failed when lastAttemptAt is not null => Failed,
            DbStatusCodes.NotificationStatus.Accepted => Pending,
            DbStatusCodes.NotificationStatus.Failed => Pending,
            DbStatusCodes.NotificationStatus.Cancelled => Cancelled,
            DbStatusCodes.NotificationStatus.Queued => Pending,
            DbStatusCodes.NotificationStatus.Sending => Pending,
            _ => Pending
        };
}
