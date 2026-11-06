using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Dtos.MqttDtos;
using Application.Interfaces.Services;
using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

/// <summary>
/// SS-P9-020 PostgreSQL path glue for BE-T-10: command + alarm + disabled account.
/// Uses Recording MQTT / Fake FCM only — never claims live Mosquitto or Firebase evidence.
/// </summary>
[Collection(nameof(AuthHttpCollection))]
public sealed class BackendAcceptancePathTests : IAsyncLifetime
{
    private readonly RecordingMqttOutboundPublisher _publisher = new();
    private readonly AuthApiFactory _factory;
    private HttpClient _client = null!;

    public BackendAcceptancePathTests()
    {
        _factory = new AuthApiFactory(services =>
        {
            services.AddSingleton<IMqttOutboundPublisher>(_publisher);
        });
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateJsonClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Post_Command_Outbox_FakePublish_Ack_Updates_State_And_History_Without_Http_Claiming_Arm()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-acc-cmd-01", "مسار أمر");

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var post = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, post.StatusCode);
        var accepted = await AuthApiFactory.ReadDataAsync(post);
        var commandId = accepted.GetProperty("commandId").GetGuid();
        Assert.Equal(DbStatusCodes.CommandStatus.Pending, accepted.GetProperty("status").GetString());
        Assert.False(accepted.GetProperty("actualStateChanged").GetBoolean());

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(DbStatusCodes.Arm.Disarmed, (await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId)).ArmState);
            Assert.NotNull(await db.OutboxMessages.SingleOrDefaultAsync(m =>
                m.AggregateId == commandId
                && m.MessageType == ExecuteCommandDraft.OutboxPublishMessageType
                && m.ProcessedAt == null));

            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            Assert.True(await dispatch.DispatchPendingCommandsAsync() >= 1);
        }

        Assert.Single(_publisher.Messages);
        Assert.Equal(ExecuteCommandDraft.Topics.Commands(deviceId), _publisher.Messages[0].Topic);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var beforeAck = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            Assert.Equal(DbStatusCodes.Arm.Disarmed, beforeAck.ArmState);
            Assert.NotNull((await db.DeviceCommands.SingleAsync(c => c.Id == commandId)).SentAt);
        }

        var ack = await IngestCommandAckAsync(
            deviceId,
            commandId,
            ownershipVersion: 1,
            status: DbStatusCodes.CommandStatus.Executed,
            resultStateSequence: 4,
            arm: DbStatusCodes.Arm.Armed);
        Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, ack.Outcome);
        Assert.True(ack.CommandClosed);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
            Assert.Equal(DbStatusCodes.CommandStatus.Executed, command.Status);
            Assert.Equal(4, command.ResultStateSequence);
            Assert.NotNull(command.AcknowledgedAt);

            var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            Assert.Equal(DbStatusCodes.Arm.Armed, state.ArmState);
            Assert.Equal(4, state.StateSequence);

            var history = await db.CommandStatusHistory
                .Where(h => h.CommandId == commandId)
                .Select(h => h.ToStatus)
                .ToListAsync();
            Assert.Contains(DbStatusCodes.CommandStatus.Pending, history);
            Assert.Contains(DbStatusCodes.CommandStatus.Executed, history);
        }
    }

    [Fact]
    public async Task Alarm_Event_Persists_Notification_And_EventPersisted_Outbox_Once_On_Redelivery()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-acc-alarm-01", "مسار حادثة");
        var eventUid = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var first = await IngestAlarmAsync(deviceId, eventUid, ownershipVersion: 1);
        Assert.Equal(AlarmEventDraft.IngestOutcomes.Applied, first.Outcome);
        Assert.True(first.NotificationCreated);
        Assert.True(first.EventPersistedQueued);

        var second = await IngestAlarmAsync(deviceId, eventUid, ownershipVersion: 1);
        Assert.Equal(AlarmEventDraft.IngestOutcomes.DuplicateConfirmed, second.Outcome);
        Assert.False(second.EventCreated);
        Assert.False(second.NotificationCreated);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.AlarmEvents.CountAsync(e => e.DeviceId == deviceId && e.EventUid == eventUid));
        Assert.Equal(1, await db.Notifications.CountAsync(n =>
            n.AlarmEventId != null
            && n.RecipientUserId == AuthApiFactory.OwnerId
            && n.NotificationType == DbStatusCodes.NotificationType.Alarm));
        Assert.Equal(1, await db.OutboxMessages.CountAsync(m =>
            m.MessageType == EventPersistedDraft.OutboxPublishMessageType
            && m.AggregateType == EventPersistedDraft.OutboxAggregateType));
    }

    [Fact]
    public async Task Disabled_Owner_Cannot_Post_Command_Even_With_Prior_Access_Token()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-acc-disabled-01", "حساب معطّل");

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var disable = await adminClient.PostAsync($"/api/admin/owners/{AuthApiFactory.OwnerId}/disable", null);
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

        var denied = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.DeviceCommands.CountAsync(c => c.DeviceId == deviceId));
    }

    private async Task<DeviceInboundIngestResultDto> IngestCommandAckAsync(
        Guid deviceId,
        Guid commandId,
        long ownershipVersion,
        string status,
        long resultStateSequence,
        string arm)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
        return await ingest.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = deviceId,
            AuthenticatedDeviceId = deviceId,
            Topic = MqttInboundDraft.Topics.Acks(deviceId),
            PayloadJson = JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                deviceId,
                messageId = Guid.NewGuid(),
                messageType = MqttInboundDraft.MessageTypes.CommandAck,
                bootId = "boot-acc",
                commandId,
                ownershipVersion,
                status,
                resultStateSequence,
                armState = arm,
                sirenState = DbStatusCodes.Siren.Off,
                healthState = DbStatusCodes.Diagnostic.Unknown,
                currentAlarmEventUid = (Guid?)null,
                clockTrusted = false
            })
        });
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
                schemaVersion = AlarmEventDraft.SchemaVersion,
                deviceId,
                messageId = Guid.NewGuid(),
                messageType = AlarmEventDraft.MessageTypes.AlarmEvent,
                bootId = "boot-acc",
                eventUid,
                ownershipVersion,
                sensorKey = "vibration-1",
                wasBuffered = false,
                clockTrusted = true,
                occurredDeviceAt = DateTimeOffset.Parse("2026-11-06T10:00:00Z")
            })
        });
    }

    private async Task<Guid> RegisterActivateAndOnlineAsync(HttpClient adminClient, string hardwareUid, string displayName)
    {
        var response = await adminClient.PostAsJsonAsync("/api/admin/devices", new
        {
            hardwareUid,
            displayName,
            credential = new
            {
                authType = DbStatusCodes.AuthType.PasswordHash,
                password = "MqttSecret!99"
            },
            settings = new
            {
                eventBufferCapacity = 64,
                commandTtlSeconds = 120,
                alarmRule = new { schemaVersion = 1 }
            }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var deviceId = (await AuthApiFactory.ReadDataAsync(response)).GetProperty("id").GetGuid();

        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
        var ownershipVersion = (await AuthApiFactory.ReadDataAsync(link)).GetProperty("ownershipVersion").GetInt64();

        await using var scope = _factory.Services.CreateAsyncScope();
        var ownerships = scope.ServiceProvider.GetRequiredService<IDeviceOwnershipService>();
        Assert.True((await ownerships.ConfirmOwnershipVersionAsync(deviceId, ownershipVersion)).Activated);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        state.LastSeenAt = DateTimeOffset.UtcNow;
        state.ArmState = DbStatusCodes.Arm.Disarmed;
        state.SirenState = DbStatusCodes.Siren.Off;
        state.StateSequence = 1;
        await db.SaveChangesAsync();
        return deviceId;
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
