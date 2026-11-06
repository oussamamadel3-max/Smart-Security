using Application.Services.NotificationServices;
using Domain.Constants;
using Domain.Entities.Notifications;

namespace Application.Tests;

public sealed class FcmPayloadBuilderTests
{
    [Fact]
    public void Alarm_Payload_Contains_Only_Identifiers()
    {
        var notification = new Notification
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            NotificationType = DbStatusCodes.NotificationType.Alarm,
            AlarmEventId = Guid.Parse("22222222-2222-2222-2222-222222222222")
        };
        var deviceId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var payload = FcmPayloadBuilder.Build(
            notification,
            deviceId,
            notification.AlarmEventId,
            null,
            null);

        Assert.Equal(4, payload.Data.Count);
        Assert.Equal(notification.Id.ToString("D"), payload.Data[FcmPayloadBuilder.KeyNotificationId]);
        Assert.Equal(DbStatusCodes.NotificationType.Alarm, payload.Data[FcmPayloadBuilder.KeyNotificationType]);
        Assert.Equal(notification.AlarmEventId.Value.ToString("D"), payload.Data[FcmPayloadBuilder.KeyAlarmEventId]);
        Assert.Equal(deviceId.ToString("D"), payload.Data[FcmPayloadBuilder.KeyDeviceId]);
        Assert.False(payload.Data.ContainsKey(FcmPayloadBuilder.KeyAdminAlertId));
        Assert.All(payload.Data.Keys, k => Assert.False(k is "title" or "body" or "wifiPassword"));
    }

    [Fact]
    public void AdminAlert_Payload_Omits_Device_Fields()
    {
        var alertId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var notification = new Notification
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            NotificationType = DbStatusCodes.NotificationType.AdminAlert,
            AdminAlertId = alertId
        };

        var payload = FcmPayloadBuilder.Build(notification, null, null, null, alertId);

        Assert.Equal(3, payload.Data.Count);
        Assert.Equal(alertId.ToString("D"), payload.Data[FcmPayloadBuilder.KeyAdminAlertId]);
        Assert.False(payload.Data.ContainsKey(FcmPayloadBuilder.KeyDeviceId));
        Assert.False(payload.Data.ContainsKey(FcmPayloadBuilder.KeyAlarmEventId));
    }
}
