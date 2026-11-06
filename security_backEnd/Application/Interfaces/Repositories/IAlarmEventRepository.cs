using Domain.Entities.Devices;
using Domain.Entities.Events;
using Domain.Entities.Notifications;
using Domain.Entities.Operations;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Persistence for alarm events, related notifications, faults, and loss reports (T-06).
/// </summary>
public interface IAlarmEventRepository
{
    Task<AlarmEvent?> FindByDeviceAndEventUidAsync(
        Guid deviceId,
        Guid eventUid,
        CancellationToken ct = default);

    Task<AlarmEvent?> GetByIdAsync(Guid alarmEventId, CancellationToken ct = default);

    void AddAlarmEvent(AlarmEvent alarmEvent);

    void AddNotification(Notification notification);

    Task<Notification?> FindAlarmNotificationAsync(
        Guid recipientUserId,
        Guid alarmEventId,
        CancellationToken ct = default);

    Task<Notification?> GetNotificationForUpdateAsync(Guid notificationId, CancellationToken ct = default);

    Task<DeviceOwnership?> FindOwnershipByVersionForUpdateAsync(
        Guid deviceId,
        long ownershipVersion,
        CancellationToken ct = default);

    Task<DeviceSensor?> FindSensorByKeyAsync(
        Guid deviceId,
        string sensorKey,
        CancellationToken ct = default);

    void AddOutboxMessage(OutboxMessage message);

    /// <summary>
    /// Loads the durable outbox row for a message type + aggregate under <c>FOR UPDATE</c>.
    /// Unique index <c>ux_outbox_messages_type_aggregate</c> allows at most one such row.
    /// </summary>
    Task<OutboxMessage?> FindOutboxByTypeAndAggregateForUpdateAsync(
        string messageType,
        Guid aggregateId,
        CancellationToken ct = default);

    Task<EventLossReport?> FindLossReportAsync(
        Guid deviceId,
        Guid reportUid,
        CancellationToken ct = default);

    void AddEventLossReport(EventLossReport report);

    Task<DeviceFault?> FindFaultAsync(
        Guid deviceId,
        Guid faultUid,
        CancellationToken ct = default);

    Task<DeviceFault?> GetFaultByIdAsync(Guid deviceFaultId, CancellationToken ct = default);

    void AddDeviceFault(DeviceFault fault);

    Task<Notification?> FindFaultNotificationAsync(
        Guid recipientUserId,
        Guid deviceFaultId,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels QUEUED/SENDING ALARM and FAULT notifications for a closed ownership period.
    /// Never touches ADMIN_ALERT rows.
    /// </summary>
    Task<int> CancelPendingAlarmAndFaultNotificationsForOwnershipAsync(
        Guid ownershipId,
        Guid recipientUserId,
        DateTimeOffset cancelledAt,
        string cancellationReason,
        CancellationToken ct = default);

    Task<IReadOnlyList<AlarmEvent>> ListForOwnershipAsync(
        Guid deviceId,
        Guid ownershipId,
        CancellationToken ct = default);

    Task<AlarmEvent?> GetForOwnershipAsync(
        Guid deviceId,
        Guid ownershipId,
        Guid alarmEventId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Notification>> ListForRecipientAsync(
        Guid recipientUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Clears tracked inserts after a unique-constraint race so the ambient transaction can continue.
    /// </summary>
    void Detach(object entity);

    /// <summary>
    /// Drops all tracked entities after a failed SaveChanges so a recovery transaction can reload cleanly.
    /// </summary>
    void ClearChangeTracker();
}
