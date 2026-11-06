namespace Application.Interfaces.Services;

/// <summary>
/// Re-checks ALARM/FAULT recipient ownership before FCM send (FR-024 / T-06 prep for T-09).
/// Transfer/unlink bulk cancel is performed inside <c>IDeviceOwnershipService</c> (T-07).
/// </summary>
public interface INotificationRecipientGuard
{
    /// <summary>
    /// Returns true when the notification may still be sent.
    /// Otherwise marks it <c>CANCELLED</c> with an approved reason and returns false.
    /// </summary>
    Task<bool> EnsureRecipientStillEligibleAsync(
        Guid notificationId,
        CancellationToken ct = default);
}
