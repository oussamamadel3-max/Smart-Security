using Application.Interfaces.Repositories;
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

    public void Detach(object entity)
        => _db.Entry(entity).State = EntityState.Detached;

    public void ClearChangeTracker()
        => _db.ChangeTracker.Clear();
}
