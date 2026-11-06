using Application.Dtos.FcmDtos;
using Domain.Constants;
using Domain.Entities.Notifications;

namespace Application.Services.NotificationServices;

/// <summary>
/// Builds identifier-only FCM data maps (FR-024 / FCM-05). No titles, bodies, or secrets.
/// </summary>
public static class FcmPayloadBuilder
{
    public const string KeyNotificationId = "notificationId";
    public const string KeyNotificationType = "notificationType";
    public const string KeyAlarmEventId = "alarmEventId";
    public const string KeyDeviceFaultId = "deviceFaultId";
    public const string KeyAdminAlertId = "adminAlertId";
    public const string KeyDeviceId = "deviceId";

    public static FcmDataPayload Build(
        Notification notification,
        Guid? deviceId,
        Guid? alarmEventId,
        Guid? deviceFaultId,
        Guid? adminAlertId)
    {
        var data = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [KeyNotificationId] = notification.Id.ToString("D"),
            [KeyNotificationType] = notification.NotificationType
        };

        switch (notification.NotificationType)
        {
            case DbStatusCodes.NotificationType.Alarm:
                if (alarmEventId is Guid ae)
                    data[KeyAlarmEventId] = ae.ToString("D");
                if (deviceId is Guid d1)
                    data[KeyDeviceId] = d1.ToString("D");
                break;
            case DbStatusCodes.NotificationType.Fault:
                if (deviceFaultId is Guid df)
                    data[KeyDeviceFaultId] = df.ToString("D");
                if (deviceId is Guid d2)
                    data[KeyDeviceId] = d2.ToString("D");
                break;
            case DbStatusCodes.NotificationType.AdminAlert:
                if (adminAlertId is Guid aa)
                    data[KeyAdminAlertId] = aa.ToString("D");
                break;
        }

        return new FcmDataPayload { Data = data };
    }
}
