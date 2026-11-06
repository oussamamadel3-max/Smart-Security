using Domain.Entities.Notifications;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Queue claiming and delivery persistence for the FCM worker (T-09).
/// </summary>
public interface INotificationDispatchRepository
{
    /// <summary>
    /// Claims due QUEUED/SENDING rows with <c>FOR UPDATE SKIP LOCKED</c>, marks them SENDING,
    /// and returns tracked entities for the ambient transaction.
    /// </summary>
    Task<IReadOnlyList<Notification>> ClaimDueBatchAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken ct = default);

    Task<Notification?> GetForUpdateAsync(Guid notificationId, CancellationToken ct = default);

    Task<short> GetMaxAttemptNoAsync(
        Guid notificationId,
        Guid pushTokenId,
        CancellationToken ct = default);

    void AddDelivery(NotificationDelivery delivery);

    Task<int> CountProviderAttemptsAsync(Guid notificationId, CancellationToken ct = default);

    Task<(Guid? DeviceId, Guid? AlarmEventId, Guid? DeviceFaultId, Guid? AdminAlertId)> ResolveSourceIdsAsync(
        Notification notification,
        CancellationToken ct = default);
}
