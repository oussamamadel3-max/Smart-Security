using Application.Dtos.OwnerDtos;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Exceptions;

namespace Application.Services.OwnerServices;

public sealed class OwnerDeviceHistoryService : IOwnerDeviceHistoryService
{
    private readonly IDeviceRepository _devices;
    private readonly IAlarmEventRepository _alarms;

    public OwnerDeviceHistoryService(IDeviceRepository devices, IAlarmEventRepository alarms)
    {
        _devices = devices;
        _alarms = alarms;
    }

    public async Task<IReadOnlyList<OwnerAlarmEventListItemDto>> ListAlarmEventsAsync(
        Guid ownerUserId,
        Guid deviceId,
        CancellationToken ct = default)
    {
        var row = await _devices.GetOpenOwnershipDeviceForOwnerAsync(ownerUserId, deviceId, ct)
            ?? throw new NotFoundException("الجهاز غير موجود ضمن ملكيتك.", "DEVICE_NOT_FOUND");

        var events = await _alarms.ListForOwnershipAsync(deviceId, row.Ownership.Id, ct);
        return events.Select(MapAlarm).ToList();
    }

    public async Task<OwnerAlarmEventListItemDto> GetAlarmEventAsync(
        Guid ownerUserId,
        Guid deviceId,
        Guid alarmEventId,
        CancellationToken ct = default)
    {
        var row = await _devices.GetOpenOwnershipDeviceForOwnerAsync(ownerUserId, deviceId, ct)
            ?? throw new NotFoundException("الجهاز غير موجود ضمن ملكيتك.", "DEVICE_NOT_FOUND");

        var alarm = await _alarms.GetForOwnershipAsync(deviceId, row.Ownership.Id, alarmEventId, ct)
            ?? throw new NotFoundException("الحادثة غير موجودة.", "ALARM_EVENT_NOT_FOUND");

        return MapAlarm(alarm);
    }

    public async Task<IReadOnlyList<OwnerNotificationListItemDto>> ListNotificationsAsync(
        Guid ownerUserId,
        CancellationToken ct = default)
    {
        var rows = await _alarms.ListForRecipientAsync(ownerUserId, ct);
        return rows.Select(n => new OwnerNotificationListItemDto
        {
            NotificationId = n.Id,
            NotificationType = n.NotificationType,
            Status = n.Status,
            CreatedAt = n.CreatedAt,
            CancelledAt = n.CancelledAt,
            CancellationReason = n.CancellationReason,
            AlarmEventId = n.AlarmEventId,
            DeviceFaultId = n.DeviceFaultId,
            AdminAlertId = n.AdminAlertId
        }).ToList();
    }

    private static OwnerAlarmEventListItemDto MapAlarm(Domain.Entities.Events.AlarmEvent alarm)
        => new()
        {
            AlarmEventId = alarm.Id,
            DeviceId = alarm.DeviceId,
            EventUid = alarm.EventUid,
            OwnershipId = alarm.OwnershipId ?? Guid.Empty,
            OwnershipVersion = alarm.OwnershipVersion,
            Status = alarm.Status,
            ReceivedAt = alarm.ReceivedAt,
            OccurredDeviceAt = alarm.OccurredDeviceAt,
            ClockTrusted = alarm.ClockTrusted,
            WasBuffered = alarm.WasBuffered,
            MutedAt = alarm.MutedAt,
            SensorKeySnapshot = alarm.SensorKeySnapshot
        };
}
