namespace Application.Interfaces.Services;

/// <summary>
/// Consumes QUEUED notifications, applies recipient guard, calls <see cref="IFcmProvider"/>,
/// and writes <c>notification_deliveries</c>. Callable without the hosted worker for tests.
/// </summary>
public interface INotificationDispatchService
{
    /// <summary>
    /// Claims and processes up to one batch of due notifications.
    /// Returns how many notifications were claimed (not how many ACCEPTED).
    /// </summary>
    Task<int> DispatchPendingAsync(CancellationToken ct = default);
}
