using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Dtos.FcmDtos;
using Application.Dtos.MqttDtos;
using Application.Interfaces.Services;
using Application.Options;
using Application.Services.NotificationServices;
using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Notifications;
using Infrastructure.Persistence;
using Infrastructure.Services.Fcm;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

/// <summary>
/// BE-T-09 / FCM-03…FCM-06 on PostgreSQL with FakeFcmProvider only — no live Firebase traffic.
/// </summary>
[Collection(nameof(AuthHttpCollection))]
public sealed class FcmNotificationDispatchTests : IAsyncLifetime
{
    private readonly AuthApiFactory _factory;
    private HttpClient _client = null!;

    public FcmNotificationDispatchTests()
    {
        _factory = new AuthApiFactory();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateJsonClient();
        ResolveFake().Reset();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task PushToken_Register_Update_Revoke_Is_Owner_Scoped()
    {
        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);

        var register = await owner.PostAsJsonAsync("/api/me/push-tokens", new
        {
            token = "owner-fcm-token-0001-abcdefgh",
            platform = DbStatusCodes.PushPlatform.Android
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var created = await AuthApiFactory.ReadDataAsync(register);
        var tokenId = created.GetProperty("pushTokenId").GetGuid();
        Assert.True(created.GetProperty("isActive").GetBoolean());

        var list = await owner.GetAsync("/api/me/push-tokens");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var items = await AuthApiFactory.ReadDataAsync(list);
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Contains(items.EnumerateArray(), e => e.GetProperty("pushTokenId").GetGuid() == tokenId);

        // Other account cannot revoke by id (not found for that user).
        var foreignRevoke = await admin.DeleteAsync($"/api/me/push-tokens/{tokenId}");
        Assert.Equal(HttpStatusCode.NotFound, foreignRevoke.StatusCode);

        var revoke = await owner.DeleteAsync($"/api/me/push-tokens/{tokenId}");
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.PushTokens.SingleAsync(t => t.Id == tokenId);
        Assert.NotNull(row.RevokedAt);
        Assert.Equal(AuthApiFactory.OwnerId, row.UserId);
    }

    [Fact]
    public async Task Dispatch_Accepted_Writes_Deliveries_And_LastAttempt_Without_Marking_Read()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-fcm-ok-01", "FCM OK");
        await RegisterTokenAsync(owner, "owner-fcm-ok-token-aaaaaaa");

        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1);
        Assert.True(ingest.NotificationCreated);
        var notificationId = ingest.NotificationId!.Value;

        ResolveFake().SetDefaultResult(FcmSendResult.Accepted("fake-ok-1"));
        var claimed = await DispatchAsync();
        Assert.Equal(1, claimed);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notification = await db.Notifications.SingleAsync(n => n.Id == notificationId);
        Assert.Equal(DbStatusCodes.NotificationStatus.Accepted, notification.Status);
        Assert.NotNull(notification.LastAttemptAt);

        var deliveries = await db.NotificationDeliveries.Where(d => d.NotificationId == notificationId).ToListAsync();
        Assert.Single(deliveries);
        Assert.Equal(DbStatusCodes.ProviderResult.Accepted, deliveries[0].ProviderResult);
        Assert.Equal(1, deliveries[0].AttemptNo);

        // Alarm event still present — FCM failure/success never deletes the incident.
        Assert.NotNull(await db.AlarmEvents.SingleOrDefaultAsync(a => a.Id == ingest.AlarmEventId));

        var detail = await owner.GetAsync($"/api/owner/notifications/{notificationId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var body = await AuthApiFactory.ReadDataAsync(detail);
        Assert.Equal(DbStatusCodes.NotificationStatus.Accepted, body.GetProperty("status").GetString());
        Assert.True(body.TryGetProperty("alarmEvent", out var alarm));
        Assert.Equal(deviceId, alarm.GetProperty("deviceId").GetGuid());
        // Payload builder identifiers only — no title/body fields on FCM data.
        var sent = ResolveFake().Sent.Single();
        Assert.Equal(notificationId.ToString("D"), sent.Payload.Data[FcmPayloadBuilder.KeyNotificationId]);
        Assert.False(sent.Payload.Data.ContainsKey("title"));
        Assert.False(sent.Payload.Data.ContainsKey("body"));
    }

    [Fact]
    public async Task Temporary_Failure_Retries_Then_Permanent_Failure_Sets_Failed_With_Attempt()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-fcm-retry-01", "FCM Retry");
        await RegisterTokenAsync(owner, "owner-fcm-retry-token-bbbb");

        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1);
        var notificationId = ingest.NotificationId!.Value;
        var fake = ResolveFake();

        fake.SetNextResult(FcmSendResult.Retryable("UNAVAILABLE"));
        Assert.Equal(1, await DispatchAsync());

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var n = await db.Notifications.SingleAsync(x => x.Id == notificationId);
            Assert.Equal(DbStatusCodes.NotificationStatus.Queued, n.Status);
            Assert.NotNull(n.LastAttemptAt);
            Assert.NotNull(n.NextAttemptAt);
            n.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        fake.SetNextResult(FcmSendResult.Retryable("UNAVAILABLE"));
        Assert.Equal(1, await DispatchAsync());

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var n = await db.Notifications.SingleAsync(x => x.Id == notificationId);
            n.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        fake.SetNextResult(FcmSendResult.Retryable("UNAVAILABLE"));
        Assert.Equal(1, await DispatchAsync());

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var n = await db.Notifications.SingleAsync(x => x.Id == notificationId);
            Assert.Equal(DbStatusCodes.NotificationStatus.Failed, n.Status);
            Assert.NotNull(n.LastAttemptAt);
            Assert.Null(n.NextAttemptAt);
            Assert.Equal(3, await db.NotificationDeliveries.CountAsync(d => d.NotificationId == notificationId));
            Assert.NotNull(await db.AlarmEvents.SingleOrDefaultAsync(a => a.Id == ingest.AlarmEventId));
        }
    }

    [Fact]
    public async Task Multi_Token_Creates_Delivery_Per_Token_And_Revokes_Invalid()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-fcm-multi-01", "FCM Multi");
        await RegisterTokenAsync(owner, "owner-fcm-multi-token-cccc1");
        await RegisterTokenAsync(owner, "owner-fcm-multi-token-cccc2");

        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1);
        var notificationId = ingest.NotificationId!.Value;
        var fake = ResolveFake();
        fake.EnqueueResults(
            FcmSendResult.Accepted("m1"),
            FcmSendResult.Failed("UNREGISTERED", revokeToken: true));

        Assert.Equal(1, await DispatchAsync());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var n = await db.Notifications.SingleAsync(x => x.Id == notificationId);
        Assert.Equal(DbStatusCodes.NotificationStatus.Accepted, n.Status);
        Assert.Equal(2, await db.NotificationDeliveries.CountAsync(d => d.NotificationId == notificationId));
        Assert.Equal(1, await db.PushTokens.CountAsync(t => t.UserId == AuthApiFactory.OwnerId && t.RevokedAt != null));
        Assert.Equal(1, await db.PushTokens.CountAsync(t => t.UserId == AuthApiFactory.OwnerId && t.RevokedAt == null));
    }

    [Fact]
    public async Task Ownership_Transfer_During_Queue_Cancels_Alarm_But_AdminAlert_Still_Sends()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-fcm-xfer-01", "FCM Xfer");
        await RegisterTokenAsync(owner, "owner-fcm-xfer-token-dddd");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_fcm_xfer");

        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1);
        var alarmNotificationId = ingest.NotificationId!.Value;

        Guid adminAlertNotificationId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var alertId = Guid.NewGuid();
            db.AdminAlerts.Add(new AdminAlert
            {
                Id = alertId,
                SenderUserId = AuthApiFactory.AdminId,
                RecipientUserId = AuthApiFactory.OwnerId,
                Title = "مستقل عن النقل",
                Body = "يجب أن يُرسل",
                CreatedAt = DateTimeOffset.UtcNow
            });
            adminAlertNotificationId = Guid.NewGuid();
            db.Notifications.Add(new Notification
            {
                Id = adminAlertNotificationId,
                RecipientUserId = AuthApiFactory.OwnerId,
                NotificationType = DbStatusCodes.NotificationType.AdminAlert,
                AdminAlertId = alertId,
                Status = DbStatusCodes.NotificationStatus.Queued,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        ResolveFake().SetDefaultResult(FcmSendResult.Accepted("after-xfer"));
        var claimed = await DispatchAsync();
        Assert.True(claimed >= 1);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var alarm = await db.Notifications.SingleAsync(n => n.Id == alarmNotificationId);
            Assert.Equal(DbStatusCodes.NotificationStatus.Cancelled, alarm.Status);
            Assert.Null(alarm.LastAttemptAt);
            Assert.Empty(await db.NotificationDeliveries.Where(d => d.NotificationId == alarmNotificationId).ToListAsync());

            var adminNotif = await db.Notifications.SingleAsync(n => n.Id == adminAlertNotificationId);
            Assert.Equal(DbStatusCodes.NotificationStatus.Accepted, adminNotif.Status);
            Assert.NotNull(adminNotif.LastAttemptAt);
            Assert.Single(await db.NotificationDeliveries.Where(d => d.NotificationId == adminAlertNotificationId).ToListAsync());
        }

        // Prior owner loses detail access after transfer.
        var denied = await owner.GetAsync($"/api/owner/notifications/{alarmNotificationId}");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task No_Active_Token_Does_Not_Stamp_LastAttempt_Or_Invent_Accepted()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-fcm-notoken-01", "FCM NoToken");
        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1);
        var notificationId = ingest.NotificationId!.Value;

        Assert.Equal(1, await DispatchAsync());
        Assert.Equal(0, ResolveFake().CallCount);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var n = await db.Notifications.SingleAsync(x => x.Id == notificationId);
        Assert.Equal(DbStatusCodes.NotificationStatus.Queued, n.Status);
        Assert.Null(n.LastAttemptAt);
        Assert.NotEqual(DbStatusCodes.NotificationStatus.Accepted, n.Status);
        Assert.NotEqual(DbStatusCodes.NotificationStatus.Failed, n.Status);
        Assert.Empty(await db.NotificationDeliveries.Where(d => d.NotificationId == notificationId).ToListAsync());
    }

    [Fact]
    public async Task Corrupt_Ciphertext_Revokes_Token_Without_Provider_Call_Or_LastAttempt()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-fcm-corrupt-01", "FCM Corrupt");
        await RegisterTokenAsync(owner, "owner-fcm-corrupt-token-eeee");

        Guid tokenId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var token = await db.PushTokens.SingleAsync(t => t.UserId == AuthApiFactory.OwnerId && t.RevokedAt == null);
            tokenId = token.Id;
            token.TokenCiphertext = new byte[] { 1, 2, 3, 4, 5 }; // truncated / invalid AES-GCM payload
            await db.SaveChangesAsync();
        }

        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1);
        var notificationId = ingest.NotificationId!.Value;
        ResolveFake().Reset();

        Assert.Equal(1, await DispatchAsync());
        Assert.Equal(0, ResolveFake().CallCount);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var n = await db.Notifications.SingleAsync(x => x.Id == notificationId);
            Assert.Equal(DbStatusCodes.NotificationStatus.Queued, n.Status);
            Assert.Null(n.LastAttemptAt);
            Assert.Empty(await db.NotificationDeliveries.Where(d => d.NotificationId == notificationId).ToListAsync());

            var token = await db.PushTokens.SingleAsync(t => t.Id == tokenId);
            Assert.NotNull(token.RevokedAt);
        }
    }

    [Fact]
    public async Task Transfer_After_Claim_Before_Send_Prevents_Provider_Call()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-fcm-race-01", "FCM Race");
        await RegisterTokenAsync(owner, "owner-fcm-race-token-ffff");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_fcm_race");

        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1);
        var notificationId = ingest.NotificationId!.Value;

        // Simulate claimed/SENDING row still awaiting provider (worker crash / lease).
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var n = await db.Notifications.SingleAsync(x => x.Id == notificationId);
            n.Status = DbStatusCodes.NotificationStatus.Sending;
            n.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        ResolveFake().Reset();
        await DispatchAsync();
        Assert.Equal(0, ResolveFake().CallCount);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var n = await db.Notifications.SingleAsync(x => x.Id == notificationId);
            Assert.Equal(DbStatusCodes.NotificationStatus.Cancelled, n.Status);
            Assert.Null(n.LastAttemptAt);
            Assert.Empty(await db.NotificationDeliveries.Where(d => d.NotificationId == notificationId).ToListAsync());
        }
    }

    [Fact]
    public async Task InFlight_Provider_Accept_Cannot_Be_Unsent_But_Detail_Denies_Prior_Owner()
    {
        // FR-024: once FCM accept happens in-flight, it cannot be revoked.
        // Transfer waiting on the finalize lock may lose the CANCELLED race; detail API still re-checks ownership.
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-fcm-inflight-01", "FCM InFlight");
        await RegisterTokenAsync(owner, "owner-fcm-inflight-token-gggg");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_fcm_inflight");

        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1);
        var notificationId = ingest.NotificationId!.Value;

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fake = ResolveFake();
        fake.Reset();
        fake.SetDefaultResult(FcmSendResult.Accepted("would-be-accepted"));
        fake.BeforeSendAsync = async _ =>
        {
            entered.TrySetResult();
            await release.Task;
        };

        var dispatchTask = DispatchAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        release.TrySetResult();
        Assert.Equal(1, await dispatchTask);
        Assert.True(fake.CallCount >= 1);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var n = await db.Notifications.SingleAsync(x => x.Id == notificationId);
            Assert.Contains(
                n.Status,
                new[]
                {
                    DbStatusCodes.NotificationStatus.Accepted,
                    DbStatusCodes.NotificationStatus.Cancelled
                });
            if (n.Status == DbStatusCodes.NotificationStatus.Accepted)
            {
                Assert.NotNull(n.LastAttemptAt);
                var deliveries = await db.NotificationDeliveries
                    .Where(d => d.NotificationId == notificationId)
                    .ToListAsync();
                Assert.NotEmpty(deliveries);
                Assert.All(deliveries, d => Assert.StartsWith("fake-", d.ProviderMessageId ?? string.Empty));
            }
        }

        var denied = await owner.GetAsync($"/api/owner/notifications/{notificationId}");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task After_Transfer_New_Alarm_Does_Not_Notify_Prior_Owner()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        using var priorOwner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-fcm-postxfer-01", "FCM PostXfer");
        await RegisterTokenAsync(priorOwner, "owner-fcm-postxfer-token-iiii");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_fcm_postxfer");

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        // Activate new ownership on device (confirm version 2).
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var telemetry = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
            var state = await telemetry.IngestAsync(new DeviceInboundEnvelope
            {
                TopicDeviceId = deviceId,
                AuthenticatedDeviceId = deviceId,
                Topic = MqttInboundDraft.Topics.State(deviceId),
                PayloadJson = JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    deviceId,
                    messageId = Guid.NewGuid(),
                    messageType = MqttInboundDraft.MessageTypes.State,
                    bootId = "boot-postxfer",
                    stateSequence = 2,
                    confirmedOwnershipVersion = 2,
                    armState = DbStatusCodes.Arm.Disarmed,
                    sirenState = DbStatusCodes.Siren.Off,
                    healthState = DbStatusCodes.Diagnostic.Unknown,
                    currentAlarmEventUid = (Guid?)null,
                    clockTrusted = false
                })
            });
            Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, state.Outcome);
        }

        ResolveFake().Reset();
        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 2);
        Assert.True(ingest.NotificationCreated);
        Assert.Equal(newOwnerId, (await GetNotificationRecipientAsync(ingest.NotificationId!.Value)));

        await DispatchAsync();
        // Prior owner's token must not receive the new-owner notification.
        Assert.DoesNotContain(
            ResolveFake().Sent,
            s => s.RawToken == "owner-fcm-postxfer-token-iiii");
    }

    [Fact]
    public async Task Fake_Accepted_MessageIds_Are_Prefixed_Not_Firebase_Evidence()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-fcm-prefix-01", "FCM Prefix");
        await RegisterTokenAsync(owner, "owner-fcm-prefix-token-hhhh");
        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1);
        ResolveFake().SetDefaultResult(FcmSendResult.Accepted("projects/demo/messages/1"));
        Assert.Equal(1, await DispatchAsync());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var delivery = await db.NotificationDeliveries.SingleAsync(d => d.NotificationId == ingest.NotificationId);
        Assert.StartsWith("fake-", delivery.ProviderMessageId);
        Assert.Equal(FcmOptions.ProviderFake, ResolveFake().ProviderName);
    }

    private FakeFcmProvider ResolveFake()
        => _factory.Services.GetRequiredService<FakeFcmProvider>();

    private async Task<int> DispatchAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dispatch = scope.ServiceProvider.GetRequiredService<INotificationDispatchService>();
        return await dispatch.DispatchPendingAsync();
    }

    private async Task<Guid> GetNotificationRecipientAsync(Guid notificationId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Notifications
            .Where(n => n.Id == notificationId)
            .Select(n => n.RecipientUserId)
            .SingleAsync();
    }

    private static async Task RegisterTokenAsync(HttpClient client, string token)
    {
        var response = await client.PostAsJsonAsync("/api/me/push-tokens", new
        {
            token,
            platform = DbStatusCodes.PushPlatform.Android
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<AlarmEventIngestResultDto> IngestAlarmAsync(
        Guid deviceId,
        Guid eventUid,
        long ownershipVersion)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IAlarmEventIngestService>();
        return await ingest.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = deviceId,
            AuthenticatedDeviceId = deviceId,
            Topic = AlarmEventDraft.Topics.Events(deviceId),
            PayloadJson = JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                deviceId,
                messageId = Guid.NewGuid(),
                messageType = AlarmEventDraft.MessageTypes.AlarmEvent,
                bootId = "boot-fcm",
                eventUid,
                ownershipVersion,
                sensorKey = "vibration-1",
                wasBuffered = false,
                clockTrusted = true,
                occurredDeviceAt = DateTimeOffset.Parse("2026-11-06T12:00:00Z")
            })
        });
    }

    private async Task<Guid> RegisterActivateAsync(HttpClient adminClient, string hardwareUid, string displayName)
    {
        var reg = await adminClient.PostAsJsonAsync("/api/admin/devices", new
        {
            hardwareUid,
            displayName,
            credential = new
            {
                authType = DbStatusCodes.AuthType.PasswordHash,
                password = "FcmSecret!99"
            },
            settings = new
            {
                eventBufferCapacity = 64,
                commandTtlSeconds = 120,
                alarmRule = new { schemaVersion = 1 }
            }
        });
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
        var deviceId = (await AuthApiFactory.ReadDataAsync(reg)).GetProperty("id").GetGuid();

        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var telemetry = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
        var state = await telemetry.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = deviceId,
            AuthenticatedDeviceId = deviceId,
            Topic = MqttInboundDraft.Topics.State(deviceId),
            PayloadJson = JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                deviceId,
                messageId = Guid.NewGuid(),
                messageType = MqttInboundDraft.MessageTypes.State,
                bootId = "boot-fcm-activate",
                stateSequence = 1,
                confirmedOwnershipVersion = 1,
                armState = DbStatusCodes.Arm.Disarmed,
                sirenState = DbStatusCodes.Siren.Off,
                healthState = DbStatusCodes.Diagnostic.Unknown,
                currentAlarmEventUid = (Guid?)null,
                clockTrusted = false
            })
        });
        Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, state.Outcome);
        return deviceId;
    }

    private async Task<Guid> CreateExtraOwnerAsync(string userName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var existing = await users.FindByNameAsync(userName);
        if (existing is not null)
            return existing.Id;

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = id,
            UserName = userName,
            FullName = userName,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        var result = await users.CreateAsync(user, AuthApiFactory.Password);
        Assert.True(result.Succeeded);
        await users.AddToRoleAsync(user, DbStatusCodes.RoleCodes.Owner);
        return id;
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(string userName)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            userName,
            password = AuthApiFactory.Password,
            refreshDelivery = "body"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var access = (await AuthApiFactory.ReadDataAsync(login)).GetProperty("accessToken").GetString()!;
        var client = _factory.CreateJsonClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        return client;
    }
}
