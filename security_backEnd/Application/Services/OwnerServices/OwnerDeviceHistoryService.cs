using Application.Dtos.OwnerDtos;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
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

    public async Task<OwnerNotificationDetailDto> GetNotificationDetailAsync(
        Guid ownerUserId,
        Guid notificationId,
        CancellationToken ct = default)
    {
        var notification = await _alarms.GetForRecipientAsync(ownerUserId, notificationId, ct)
            ?? throw new NotFoundException("الإشعار غير موجود.", "NOTIFICATION_NOT_FOUND");

        var detail = new OwnerNotificationDetailDto
        {
            NotificationId = notification.Id,
            NotificationType = notification.NotificationType,
            Status = notification.Status,
            CreatedAt = notification.CreatedAt,
            LastAttemptAt = notification.LastAttemptAt,
            CancelledAt = notification.CancelledAt,
            CancellationReason = notification.CancellationReason
        };

        switch (notification.NotificationType)
        {
            case DbStatusCodes.NotificationType.Alarm:
            {
                if (notification.AlarmEventId is null)
                    throw new NotFoundException("الإشعار غير موجود.", "NOTIFICATION_NOT_FOUND");

                var alarm = await _alarms.GetByIdAsync(notification.AlarmEventId.Value, ct)
                    ?? throw new NotFoundException("الحادثة غير موجودة.", "ALARM_EVENT_NOT_FOUND");

                // Fresh ownership gate: former owners must not open locked-screen details (FCM-05).
                var open = await _devices.GetOpenOwnershipDeviceForOwnerAsync(ownerUserId, alarm.DeviceId, ct)
                    ?? throw new ForbiddenException("لا تملك صلاحية عرض تفاصيل هذا الإشعار.");

                var scoped = await _alarms.GetForOwnershipAsync(alarm.DeviceId, open.Ownership.Id, alarm.Id, ct)
                    ?? throw new ForbiddenException("لا تملك صلاحية عرض تفاصيل هذا الإشعار.");

                detail.DeviceId = scoped.DeviceId;
                detail.AlarmEvent = MapAlarm(scoped);
                break;
            }
            case DbStatusCodes.NotificationType.Fault:
            {
                if (notification.DeviceFaultId is null)
                    throw new NotFoundException("الإشعار غير موجود.", "NOTIFICATION_NOT_FOUND");

                var fault = await _alarms.GetFaultByIdAsync(notification.DeviceFaultId.Value, ct)
                    ?? throw new NotFoundException("العطل غير موجود.", "DEVICE_FAULT_NOT_FOUND");

                _ = await _devices.GetOpenOwnershipDeviceForOwnerAsync(ownerUserId, fault.DeviceId, ct)
                    ?? throw new ForbiddenException("لا تملك صلاحية عرض تفاصيل هذا الإشعار.");

                detail.DeviceId = fault.DeviceId;
                detail.DeviceFault = new OwnerFaultDetailDto
                {
                    DeviceFaultId = fault.Id,
                    DeviceId = fault.DeviceId,
                    FaultCode = fault.FaultCode,
                    Status = fault.Status,
                    ReceivedAt = fault.ReceivedAt
                };
                break;
            }
            case DbStatusCodes.NotificationType.AdminAlert:
            {
                if (notification.AdminAlertId is null)
                    throw new NotFoundException("الإشعار غير موجود.", "NOTIFICATION_NOT_FOUND");

                var alert = await _alarms.GetAdminAlertByIdAsync(notification.AdminAlertId.Value, ct)
                    ?? throw new NotFoundException("التنبيه الإداري غير موجود.", "ADMIN_ALERT_NOT_FOUND");

                if (alert.RecipientUserId != ownerUserId)
                    throw new ForbiddenException("لا تملك صلاحية عرض تفاصيل هذا الإشعار.");

                detail.AdminAlert = new OwnerAdminAlertDetailDto
                {
                    AdminAlertId = alert.Id,
                    Title = alert.Title,
                    Body = alert.Body,
                    CreatedAt = alert.CreatedAt
                };
                break;
            }
            default:
                throw new NotFoundException("الإشعار غير موجود.", "NOTIFICATION_NOT_FOUND");
        }

        return detail;
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
