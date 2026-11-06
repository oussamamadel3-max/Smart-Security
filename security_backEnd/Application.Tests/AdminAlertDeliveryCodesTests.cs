using Domain.Constants;

namespace Application.Tests;

public sealed class AdminAlertDeliveryCodesTests
{
    private static readonly DateTimeOffset AttemptedAt = DateTimeOffset.Parse("2026-11-06T12:00:00Z");

    [Theory]
    [InlineData(DbStatusCodes.NotificationStatus.Queued, null, AdminAlertDeliveryCodes.Pending)]
    [InlineData(DbStatusCodes.NotificationStatus.Sending, null, AdminAlertDeliveryCodes.Pending)]
    [InlineData(DbStatusCodes.NotificationStatus.Cancelled, null, AdminAlertDeliveryCodes.Cancelled)]
    [InlineData(null, null, AdminAlertDeliveryCodes.Pending)]
    public void Maps_Without_Inventing_Success(string? status, DateTimeOffset? lastAttemptAt, string expected)
    {
        Assert.Equal(expected, AdminAlertDeliveryCodes.FromNotificationStatus(status, lastAttemptAt));
    }

    [Fact]
    public void Queued_Without_Fcm_Is_Pending_Not_Accepted()
    {
        var mapped = AdminAlertDeliveryCodes.FromNotificationStatus(DbStatusCodes.NotificationStatus.Queued);
        Assert.Equal(AdminAlertDeliveryCodes.Pending, mapped);
        Assert.NotEqual(AdminAlertDeliveryCodes.Accepted, mapped);
        Assert.NotEqual(AdminAlertDeliveryCodes.Failed, mapped);
    }

    [Fact]
    public void Failed_Without_Attempt_Timestamp_Stays_Pending()
    {
        var mapped = AdminAlertDeliveryCodes.FromNotificationStatus(
            DbStatusCodes.NotificationStatus.Failed,
            lastAttemptAt: null);
        Assert.Equal(AdminAlertDeliveryCodes.Pending, mapped);
        Assert.NotEqual(AdminAlertDeliveryCodes.Failed, mapped);
    }

    [Fact]
    public void Accepted_Without_Attempt_Timestamp_Stays_Pending()
    {
        var mapped = AdminAlertDeliveryCodes.FromNotificationStatus(
            DbStatusCodes.NotificationStatus.Accepted,
            lastAttemptAt: null);
        Assert.Equal(AdminAlertDeliveryCodes.Pending, mapped);
        Assert.NotEqual(AdminAlertDeliveryCodes.Accepted, mapped);
    }

    [Fact]
    public void Failed_With_Attempt_Timestamp_Is_Failed()
    {
        Assert.Equal(
            AdminAlertDeliveryCodes.Failed,
            AdminAlertDeliveryCodes.FromNotificationStatus(
                DbStatusCodes.NotificationStatus.Failed,
                AttemptedAt));
    }

    [Fact]
    public void Accepted_With_Attempt_Timestamp_Is_Accepted()
    {
        Assert.Equal(
            AdminAlertDeliveryCodes.Accepted,
            AdminAlertDeliveryCodes.FromNotificationStatus(
                DbStatusCodes.NotificationStatus.Accepted,
                AttemptedAt));
    }
}
