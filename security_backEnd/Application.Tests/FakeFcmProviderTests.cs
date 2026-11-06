using Application.Dtos.FcmDtos;
using Application.Services.NotificationServices;
using Domain.Constants;
using Domain.Entities.Notifications;
using Infrastructure.Services.Fcm;

namespace Application.Tests;

public sealed class FakeFcmProviderTests
{
    [Fact]
    public async Task Scripts_Accepted_Retryable_And_Failed()
    {
        var fake = new FakeFcmProvider();
        fake.EnqueueResults(
            FcmSendResult.Accepted("msg-1"),
            FcmSendResult.Retryable("TEMP"),
            FcmSendResult.Failed("UNREGISTERED", revokeToken: true));

        var payload = FcmPayloadBuilder.Build(
            new Notification
            {
                Id = Guid.NewGuid(),
                NotificationType = DbStatusCodes.NotificationType.AdminAlert,
                AdminAlertId = Guid.NewGuid()
            },
            null,
            null,
            null,
            Guid.NewGuid());

        var req = new FcmSendRequest
        {
            NotificationId = Guid.NewGuid(),
            PushTokenId = Guid.NewGuid(),
            RawToken = "token-a",
            Payload = payload
        };

        Assert.Equal(DbStatusCodes.ProviderResult.Accepted, (await fake.SendAsync(req)).ProviderResult);
        Assert.Equal(DbStatusCodes.ProviderResult.Retryable, (await fake.SendAsync(req)).ProviderResult);
        var failed = await fake.SendAsync(req);
        Assert.Equal(DbStatusCodes.ProviderResult.Failed, failed.ProviderResult);
        Assert.True(failed.RevokeToken);
        Assert.Equal(3, fake.CallCount);
    }
}
