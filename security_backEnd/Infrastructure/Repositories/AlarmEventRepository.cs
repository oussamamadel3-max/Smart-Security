using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities.Devices;
using Domain.Entities.Events;
using Domain.Entities.Notifications;
using Domain.Entities.Operations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AlarmEventRepository : IAlarmEventRepository
{
    private readonly AppDbContext _db;

    public AlarmEventRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<AlarmEvent?> FindByDeviceAndEventUidAsync(
        Guid deviceId,
        Guid eventUid,
        CancellationToken ct = default)
        => _db.AlarmEvents.SingleOrDefaultAsync(
            e => e.DeviceId == deviceId && e.EventUid == eventUid,
            ct);

    public Task<AlarmEvent?> GetByIdAsync(Guid alarmEventId, CancellationToken ct = default)
        => _db.AlarmEvents.SingleOrDefaultAsync(e => e.Id == alarmEventId, ct);

    public void AddAlarmEvent(AlarmEvent alarmEvent)
        => _db.AlarmEvents.Add(alarmEvent);

    public void AddNotification(Notification notification)
        => _db.Notifications.Add(notification);

    public Task<Notification?> FindAlarmNotificationAsync(
        Guid recipientUserId,
        Guid alarmEventId,
        CancellationToken ct = default)
        => _db.Notifications.SingleOrDefaultAsync(
            n => n.RecipientUserId == recipientUserId && n.AlarmEventId == alarmEventId,
            ct);

    public async Task<Notification?> GetNotificationForUpdateAsync(
        Guid notificationId,
        CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM notifications WHERE id = {notificationId} FOR UPDATE",
            ct);

        return await _db.Notifications.SingleOrDefaultAsync(n => n.Id == notificationId, ct);
    }

    public async Task<DeviceOwnership?> FindOwnershipByVersionForUpdateAsync(
        Guid deviceId,
        long ownershipVersion,
        CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1 FROM device_ownerships
             WHERE device_id = {deviceId} AND version = {ownershipVersion}
             FOR UPDATE
             """,
            ct);

        return await _db.DeviceOwnerships.SingleOrDefaultAsync(
            o => o.DeviceId == deviceId && o.Version == ownershipVersion,
            ct);
    }

    public Task<DeviceSensor?> FindSensorByKeyAsync(
        Guid deviceId,
        string sensorKey,
        CancellationToken ct = default)
        => _db.DeviceSensors.SingleOrDefaultAsync(
            s => s.DeviceId == deviceId && s.SensorKey == sensorKey,
            ct);

    public void AddOutboxMessage(OutboxMessage message)
        => _db.OutboxMessages.Add(message);

    public async Task<OutboxMessage?> FindOutboxByTypeAndAggregateForUpdateAsync(
        string messageType,
        Guid aggregateId,
        CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1 FROM outbox_messages
             WHERE message_type = {messageType} AND aggregate_id = {aggregateId}
             FOR UPDATE
             """,
            ct);

        return await _db.OutboxMessages.SingleOrDefaultAsync(
            m => m.MessageType == messageType && m.AggregateId == aggregateId,
            ct);
    }

    public Task<EventLossReport?> FindLossReportAsync(
        Guid deviceId,
        Guid reportUid,
        CancellationToken ct = default)
        => _db.EventLossReports.SingleOrDefaultAsync(
            r => r.DeviceId == deviceId && r.ReportUid == reportUid,
            ct);

    public void AddEventLossReport(EventLossReport report)
        => _db.EventLossReports.Add(report);

    public Task<DeviceFault?> FindFaultAsync(
        Guid deviceId,
        Guid faultUid,
        CancellationToken ct = default)
        => _db.DeviceFaults.SingleOrDefaultAsync(
            f => f.DeviceId == deviceId && f.FaultUid == faultUid,
            ct);

    public Task<DeviceFault?> GetFaultByIdAsync(Guid deviceFaultId, CancellationToken ct = default)
        => _db.DeviceFaults.SingleOrDefaultAsync(f => f.Id == deviceFaultId, ct);

    public void AddDeviceFault(DeviceFault fault)
        => _db.DeviceFaults.Add(fault);

    public Task<Notification?> FindFaultNotificationAsync(
        Guid recipientUserId,
        Guid deviceFaultId,
        CancellationToken ct = default)
        => _db.Notifications.SingleOrDefaultAsync(
            n => n.RecipientUserId == recipientUserId && n.DeviceFaultId == deviceFaultId,
            ct);

    public async Task<int> CancelPendingAlarmAndFaultNotificationsForOwnershipAsync(
        Guid ownershipId,
        Guid recipientUserId,
        DateTimeOffset cancelledAt,
        string cancellationReason,
        CancellationToken ct = default)
    {
        var alarmCancelled = await (
            from n in _db.Notifications
            join a in _db.AlarmEvents on n.AlarmEventId equals a.Id
            where n.RecipientUserId == recipientUserId
                  && n.NotificationType == DbStatusCodes.NotificationType.Alarm
                  && a.OwnershipId == ownershipId
                  && (n.Status == DbStatusCodes.NotificationStatus.Queued
                      || n.Status == DbStatusCodes.NotificationStatus.Sending)
            select n
        ).ExecuteUpdateAsync(
            setters => setters
                .SetProperty(n => n.Status, DbStatusCodes.NotificationStatus.Cancelled)
                .SetProperty(n => n.CancelledAt, cancelledAt)
                .SetProperty(n => n.CancellationReason, cancellationReason)
                .SetProperty(n => n.NextAttemptAt, (DateTimeOffset?)null),
            ct);

        var faultCancelled = await (
            from n in _db.Notifications
            join f in _db.DeviceFaults on n.DeviceFaultId equals f.Id
            where n.RecipientUserId == recipientUserId
                  && n.NotificationType == DbStatusCodes.NotificationType.Fault
                  && f.OwnershipId == ownershipId
                  && (n.Status == DbStatusCodes.NotificationStatus.Queued
                      || n.Status == DbStatusCodes.NotificationStatus.Sending)
            select n
        ).ExecuteUpdateAsync(
            setters => setters
                .SetProperty(n => n.Status, DbStatusCodes.NotificationStatus.Cancelled)
                .SetProperty(n => n.CancelledAt, cancelledAt)
                .SetProperty(n => n.CancellationReason, cancellationReason)
                .SetProperty(n => n.NextAttemptAt, (DateTimeOffset?)null),
            ct);

        return alarmCancelled + faultCancelled;
    }

    public async Task<IReadOnlyList<AlarmEvent>> ListForOwnershipAsync(
        Guid deviceId,
        Guid ownershipId,
        CancellationToken ct = default)
    {
        return await _db.AlarmEvents
            .AsNoTracking()
            .Where(e => e.DeviceId == deviceId && e.OwnershipId == ownershipId)
            .OrderByDescending(e => e.ReceivedAt)
            .ToListAsync(ct);
    }

    public Task<AlarmEvent?> GetForOwnershipAsync(
        Guid deviceId,
        Guid ownershipId,
        Guid alarmEventId,
        CancellationToken ct = default)
        => _db.AlarmEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                e => e.Id == alarmEventId
                     && e.DeviceId == deviceId
                     && e.OwnershipId == ownershipId,
                ct);

    public async Task<IReadOnlyList<Notification>> ListForRecipientAsync(
        Guid recipientUserId,
        CancellationToken ct = default)
    {
        // ADMIN_ALERT: always for recipient.
        // ALARM/FAULT: only rows whose source ownership period belongs to this recipient (FR-032).
        var admin = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == recipientUserId
                        && n.NotificationType == DbStatusCodes.NotificationType.AdminAlert)
            .ToListAsync(ct);

        var alarms = await (
            from n in _db.Notifications.AsNoTracking()
            join a in _db.AlarmEvents.AsNoTracking() on n.AlarmEventId equals a.Id
            join o in _db.DeviceOwnerships.AsNoTracking() on a.OwnershipId equals o.Id
            where n.RecipientUserId == recipientUserId
                  && n.NotificationType == DbStatusCodes.NotificationType.Alarm
                  && o.OwnerUserId == recipientUserId
            select n
        ).ToListAsync(ct);

        var faults = await (
            from n in _db.Notifications.AsNoTracking()
            join f in _db.DeviceFaults.AsNoTracking() on n.DeviceFaultId equals f.Id
            join o in _db.DeviceOwnerships.AsNoTracking() on f.OwnershipId equals o.Id
            where n.RecipientUserId == recipientUserId
                  && n.NotificationType == DbStatusCodes.NotificationType.Fault
                  && o.OwnerUserId == recipientUserId
            select n
        ).ToListAsync(ct);

        return admin
            .Concat(alarms)
            .Concat(faults)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
    }

    public void AddAdminAlert(AdminAlert alert)
        => _db.AdminAlerts.Add(alert);

    public Task<AdminAlert?> GetAdminAlertByIdAsync(Guid adminAlertId, CancellationToken ct = default)
        => _db.AdminAlerts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == adminAlertId, ct);

    public Task<Notification?> FindAdminAlertNotificationAsync(
        Guid recipientUserId,
        Guid adminAlertId,
        CancellationToken ct = default)
        => _db.Notifications.AsNoTracking().SingleOrDefaultAsync(
            n => n.RecipientUserId == recipientUserId && n.AdminAlertId == adminAlertId,
            ct);

    public async Task<(IReadOnlyList<(AdminAlert Alert, Notification? Notification)> Items, long TotalCount)> QueryAdminAlertsAsync(
        Guid? recipientUserId,
        Guid? senderUserId,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = _db.AdminAlerts.AsNoTracking().AsQueryable();
        if (recipientUserId is Guid recipient)
            query = query.Where(a => a.RecipientUserId == recipient);
        if (senderUserId is Guid sender)
            query = query.Where(a => a.SenderUserId == sender);

        var total = await query.LongCountAsync(ct);
        var alerts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        if (alerts.Count == 0)
            return (Array.Empty<(AdminAlert, Notification?)>(), total);

        var alertIds = alerts.Select(a => a.Id).ToList();
        var notifications = await _db.Notifications.AsNoTracking()
            .Where(n => n.AdminAlertId != null && alertIds.Contains(n.AdminAlertId.Value))
            .ToListAsync(ct);
        var byAlert = notifications
            .Where(n => n.AdminAlertId.HasValue)
            .GroupBy(n => n.AdminAlertId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

        var items = alerts
            .Select(a => (a, byAlert.TryGetValue(a.Id, out var n) ? n : null))
            .ToList();
        return (items, total);
    }

    public async Task<(IReadOnlyList<DeviceFault> Items, long TotalCount)> QueryDeviceFaultsAsync(
        Guid? deviceId,
        string? status,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = _db.DeviceFaults.AsNoTracking().AsQueryable();
        if (deviceId is Guid id)
            query = query.Where(f => f.DeviceId == id);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(f => f.Status == status);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(f => f.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (items, total);
    }

    public void Detach(object entity)
        => _db.Entry(entity).State = EntityState.Detached;

    public void ClearChangeTracker()
        => _db.ChangeTracker.Clear();
}
