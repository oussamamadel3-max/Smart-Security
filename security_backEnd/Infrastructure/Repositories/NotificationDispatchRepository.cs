using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class NotificationDispatchRepository : INotificationDispatchRepository
{
    private readonly AppDbContext _db;

    public NotificationDispatchRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Notification>> ClaimDueBatchAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken ct = default)
    {
        var queued = DbStatusCodes.NotificationStatus.Queued;
        var sending = DbStatusCodes.NotificationStatus.Sending;

        var ids = await _db.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value"
                FROM notifications
                WHERE (
                        status = {queued}
                        AND (next_attempt_at IS NULL OR next_attempt_at <= {utcNow})
                      )
                   OR (
                        status = {sending}
                        AND next_attempt_at IS NOT NULL
                        AND next_attempt_at <= {utcNow}
                      )
                ORDER BY COALESCE(next_attempt_at, created_at), created_at
                FOR UPDATE SKIP LOCKED
                LIMIT {batchSize}
                """)
            .ToListAsync(ct);

        if (ids.Count == 0)
            return Array.Empty<Notification>();

        var rows = await _db.Notifications
            .Where(n => ids.Contains(n.Id))
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            row.Status = DbStatusCodes.NotificationStatus.Sending;
            // Hold claim briefly; next_attempt_at acts as lease if worker crashes mid-send.
            row.NextAttemptAt = utcNow.AddSeconds(120);
        }

        return rows
            .OrderBy(n => n.NextAttemptAt ?? n.CreatedAt)
            .ThenBy(n => n.CreatedAt)
            .ToList();
    }

    public async Task<Notification?> GetForUpdateAsync(Guid notificationId, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM notifications WHERE id = {notificationId} FOR UPDATE",
            ct);
        return await _db.Notifications.SingleOrDefaultAsync(n => n.Id == notificationId, ct);
    }

    public async Task<short> GetMaxAttemptNoAsync(
        Guid notificationId,
        Guid pushTokenId,
        CancellationToken ct = default)
    {
        var max = await _db.NotificationDeliveries
            .Where(d => d.NotificationId == notificationId && d.PushTokenId == pushTokenId)
            .Select(d => (short?)d.AttemptNo)
            .MaxAsync(ct);
        return max ?? 0;
    }

    public void AddDelivery(NotificationDelivery delivery)
        => _db.NotificationDeliveries.Add(delivery);

    public Task<int> CountProviderAttemptsAsync(Guid notificationId, CancellationToken ct = default)
        => _db.NotificationDeliveries.CountAsync(d => d.NotificationId == notificationId, ct);

    public async Task<(Guid? DeviceId, Guid? AlarmEventId, Guid? DeviceFaultId, Guid? AdminAlertId)> ResolveSourceIdsAsync(
        Notification notification,
        CancellationToken ct = default)
    {
        switch (notification.NotificationType)
        {
            case DbStatusCodes.NotificationType.Alarm when notification.AlarmEventId is Guid alarmId:
            {
                var deviceId = await _db.AlarmEvents.AsNoTracking()
                    .Where(a => a.Id == alarmId)
                    .Select(a => (Guid?)a.DeviceId)
                    .SingleOrDefaultAsync(ct);
                return (deviceId, alarmId, null, null);
            }
            case DbStatusCodes.NotificationType.Fault when notification.DeviceFaultId is Guid faultId:
            {
                var deviceId = await _db.DeviceFaults.AsNoTracking()
                    .Where(f => f.Id == faultId)
                    .Select(f => (Guid?)f.DeviceId)
                    .SingleOrDefaultAsync(ct);
                return (deviceId, null, faultId, null);
            }
            case DbStatusCodes.NotificationType.AdminAlert when notification.AdminAlertId is Guid alertId:
                return (null, null, null, alertId);
            default:
                return (null, notification.AlarmEventId, notification.DeviceFaultId, notification.AdminAlertId);
        }
    }
}
