namespace Application.Interfaces.Services;

/// <summary>
/// Processes durable outbox rows after commit. Live MQTT publish remains gated by options / G1.
/// </summary>
public interface IOutboxDispatchService
{
    /// <summary>
    /// Claims and attempts to publish pending <c>PUBLISH_DEVICE_COMMAND</c> rows.
    /// Returns how many rows were claimed in this cycle.
    /// </summary>
    Task<int> DispatchPendingCommandsAsync(CancellationToken ct = default);

    /// <summary>
    /// Claims and attempts to publish pending <c>PUBLISH_EVENT_PERSISTED</c> rows.
    /// Transport PUBACK is never treated as device execution; rows are durable across worker crashes.
    /// </summary>
    Task<int> DispatchPendingEventPersistedAsync(CancellationToken ct = default);

    /// <summary>
    /// Marks PENDING commands whose ACK wait elapsed as TIMED_OUT (unknown execution outcome).
    /// </summary>
    Task<int> MarkTimedOutCommandsAsync(CancellationToken ct = default);
}
