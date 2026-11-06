using Domain.Entities.Commands;
using Domain.Entities.Operations;

namespace Application.Interfaces.Repositories;

public interface IDeviceCommandRepository
{
    Task<DeviceCommand?> GetByIdAsync(Guid commandId, CancellationToken ct = default);

    Task<DeviceCommand?> GetByIdForUpdateAsync(Guid commandId, CancellationToken ct = default);

    Task<DeviceCommand?> FindByIdempotencyAsync(
        Guid requestedByUserId,
        Guid idempotencyKey,
        CancellationToken ct = default);

    Task<long> GetMaxCommandSequenceAsync(Guid deviceId, CancellationToken ct = default);

    Task<bool> HasBlockingUnresolvedCommandAsync(Guid deviceId, CancellationToken ct = default);

    Task<IReadOnlyList<DeviceCommand>> ListPendingPastAckTimeoutAsync(
        DateTimeOffset utcNow,
        int limit,
        CancellationToken ct = default);

    Task<IReadOnlyList<DeviceCommand>> ListUnreconciledTimedOutAsync(
        Guid deviceId,
        CancellationToken ct = default);

    /// <summary>
    /// PENDING and unreconciled TIMED_OUT commands for a device (cancelled inside transfer/unlink).
    /// </summary>
    Task<IReadOnlyList<DeviceCommand>> ListCancellableCommandsForDeviceForUpdateAsync(
        Guid deviceId,
        CancellationToken ct = default);

    Task<IReadOnlyList<DeviceCommand>> ListForOwnershipAsync(
        Guid deviceId,
        Guid ownershipId,
        CancellationToken ct = default);

    /// <summary>
    /// Marks unprocessed EXECUTE_COMMAND outbox rows for the given command aggregates as processed.
    /// </summary>
    Task<int> MarkCommandOutboxProcessedAsync(
        IReadOnlyCollection<Guid> commandIds,
        DateTimeOffset processedAt,
        string lastErrorCode,
        CancellationToken ct = default);

    void AddCommand(DeviceCommand command);

    void AddStatusHistory(CommandStatusHistory history);

    void AddOutboxMessage(OutboxMessage message);

    Task<OutboxMessage?> GetOutboxForUpdateAsync(Guid outboxId, CancellationToken ct = default);

    Task<IReadOnlyList<OutboxMessage>> ClaimPendingPublishBatchAsync(
        string messageType,
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken ct = default);
}
