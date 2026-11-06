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

[Collection(nameof(AuthHttpCollection))]
public sealed class DeviceCommandLifecycleTests : IAsyncLifetime
{
    private readonly RecordingMqttOutboundPublisher _publisher = new();
    private readonly AuthApiFactory _factory;
    private HttpClient _client = null!;

    public DeviceCommandLifecycleTests()
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
    public async Task Outbox_Dispatch_Publishes_ExecuteCommand_And_Sets_SentAt_Without_Changing_Arm()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-life-pub-01", "نشر");
        var commandId = await CreatePendingArmAsync(deviceId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
        var claimed = await dispatch.DispatchPendingCommandsAsync();
        Assert.True(claimed >= 1);
        Assert.Single(_publisher.Messages);

        var published = _publisher.Messages[0];
        Assert.Equal(ExecuteCommandDraft.Topics.Commands(deviceId), published.Topic);
        using (var doc = JsonDocument.Parse(published.PayloadJson))
        {
            Assert.Equal(ExecuteCommandDraft.MqttMessageType, doc.RootElement.GetProperty("messageType").GetString());
            Assert.Equal(commandId, doc.RootElement.GetProperty("commandId").GetGuid());
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
        Assert.Equal(DbStatusCodes.CommandStatus.Pending, command.Status);
        Assert.NotNull(command.SentAt);

        var outbox = await db.OutboxMessages.SingleAsync(m => m.AggregateId == commandId);
        Assert.NotNull(outbox.ProcessedAt);

        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Arm.Disarmed, state.ArmState);
    }

    [Fact]
    public async Task Outbox_Retry_After_Transient_Failure_Is_Idempotent_On_CommandId()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-life-retry-01", "إعادة");
        var commandId = await CreatePendingArmAsync(deviceId);

        _publisher.FailNext(1);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingCommandsAsync();
        }

        Assert.Empty(_publisher.Messages);

        // Simulate retry window elapsed (worker uses AvailableAt backoff after failure).
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = await db.OutboxMessages.SingleAsync(m => m.AggregateId == commandId);
            outbox.AvailableAt = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();

            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingCommandsAsync();
        }

        Assert.Single(_publisher.Messages);
        using var doc = JsonDocument.Parse(_publisher.Messages[0].PayloadJson);
        Assert.Equal(commandId, doc.RootElement.GetProperty("commandId").GetGuid());
    }

    [Fact]
    public async Task Disabled_Publisher_Does_Not_Mark_Outbox_Processed()
    {
        _publisher.IsLivePublishEnabled = false;
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-life-disabled-01", "معطل");
        var commandId = await CreatePendingArmAsync(deviceId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
        await dispatch.DispatchPendingCommandsAsync();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var outbox = await db.OutboxMessages.SingleAsync(m => m.AggregateId == commandId);
        Assert.Null(outbox.ProcessedAt);
        Assert.Equal(ExecuteCommandDraft.RejectCodes.PublishDisabled, outbox.LastErrorCode);
        Assert.Empty(_publisher.Messages);
    }

    [Fact]
    public async Task CommandAck_Closes_Pending_And_Applies_Actual_State()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-life-ack-01", "تأكيد");
        var commandId = await CreatePendingArmAsync(deviceId);

        var ack = await IngestCommandAckAsync(
            deviceId,
            commandId,
            ownershipVersion: 1,
            status: DbStatusCodes.CommandStatus.Executed,
            resultStateSequence: 5,
            arm: DbStatusCodes.Arm.Armed);

        Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, ack.Outcome);
        Assert.True(ack.CommandClosed);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
        Assert.Equal(DbStatusCodes.CommandStatus.Executed, command.Status);
        Assert.Equal(5, command.ResultStateSequence);
        Assert.NotNull(command.AcknowledgedAt);

        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Arm.Armed, state.ArmState);
        Assert.Equal(5, state.StateSequence);

        var history = await db.CommandStatusHistory
            .Where(h => h.CommandId == commandId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
        Assert.Contains(history, h => h.ToStatus == DbStatusCodes.CommandStatus.Executed && !h.IsLateAck);
    }

    [Fact]
    public async Task TimedOut_Blocks_Conflicting_Until_State_Reconciliation()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-life-to-01", "مهلة");
        var commandId = await CreatePendingArmAsync(deviceId);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
            command.CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-30);
            await db.SaveChangesAsync();

            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            var marked = await dispatch.MarkTimedOutCommandsAsync();
            Assert.True(marked >= 1);

            command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
            Assert.Equal(DbStatusCodes.CommandStatus.TimedOut, command.Status);
            Assert.Null(command.ReconciledAt);
        }

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var blocked = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Disarm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        // Newer STATE reconciles without claiming EXECUTED.
        await IngestStateAsync(deviceId, stateSequence: 3, arm: DbStatusCodes.Arm.Disarmed, confirmedOwnershipVersion: 1);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
            Assert.Equal(DbStatusCodes.CommandStatus.TimedOut, command.Status);
            Assert.NotNull(command.ReconciledAt);
        }

        var allowed = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Disarm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, allowed.StatusCode);
    }

    [Fact]
    public async Task Late_Ack_After_Timeout_Updates_Command_Without_Rewinding_Newer_State()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-life-late-01", "متأخر");
        var commandId = await CreatePendingArmAsync(deviceId);

        // Device already reported a newer STATE before the late ACK arrives.
        await IngestStateAsync(deviceId, stateSequence: 10, arm: DbStatusCodes.Arm.Disarmed, confirmedOwnershipVersion: 1);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
            command.Status = DbStatusCodes.CommandStatus.TimedOut;
            command.CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-60);
            await db.SaveChangesAsync();
        }

        var late = await IngestCommandAckAsync(
            deviceId,
            commandId,
            ownershipVersion: 1,
            status: DbStatusCodes.CommandStatus.Executed,
            resultStateSequence: 4,
            arm: DbStatusCodes.Arm.Armed);

        Assert.Equal(MqttInboundDraft.IngestOutcomes.CommandClosedOnly, late.Outcome);
        Assert.True(late.CommandClosed);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
            Assert.Equal(DbStatusCodes.CommandStatus.Executed, command.Status);
            Assert.True(await db.CommandStatusHistory.AnyAsync(h =>
                h.CommandId == commandId && h.IsLateAck));

            var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            Assert.Equal(10, state.StateSequence);
            Assert.Equal(DbStatusCodes.Arm.Disarmed, state.ArmState);
        }
    }

    [Fact]
    public async Task Ack_Then_State_Ordering_And_State_Then_Ack_Both_Keep_Highest_Sequence()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-life-order-01", "ترتيب");

        var cmdAckFirst = await CreatePendingArmAsync(deviceId);
        await IngestCommandAckAsync(deviceId, cmdAckFirst, 1, DbStatusCodes.CommandStatus.Executed, 6, DbStatusCodes.Arm.Armed);
        await IngestStateAsync(deviceId, stateSequence: 7, arm: DbStatusCodes.Arm.Armed, confirmedOwnershipVersion: 1);

        // Reconcile so a second command can be created.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // First command already EXECUTED — no block.
            Assert.False(await db.DeviceCommands.AnyAsync(c =>
                c.DeviceId == deviceId
                && (c.Status == DbStatusCodes.CommandStatus.Pending
                    || (c.Status == DbStatusCodes.CommandStatus.TimedOut && c.ReconciledAt == null))));
        }

        var cmdStateFirst = await CreatePendingArmAsync(deviceId);
        await IngestStateAsync(deviceId, stateSequence: 9, arm: DbStatusCodes.Arm.Disarmed, confirmedOwnershipVersion: 1);
        await IngestCommandAckAsync(deviceId, cmdStateFirst, 1, DbStatusCodes.CommandStatus.Executed, 8, DbStatusCodes.Arm.Armed);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            Assert.Equal(9, state.StateSequence);
            Assert.Equal(DbStatusCodes.Arm.Disarmed, state.ArmState);

            var command = await db.DeviceCommands.SingleAsync(c => c.Id == cmdStateFirst);
            Assert.Equal(DbStatusCodes.CommandStatus.Executed, command.Status);
        }
    }

    private async Task<Guid> CreatePendingArmAsync(Guid deviceId)
    {
        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var response = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return (await AuthApiFactory.ReadDataAsync(response)).GetProperty("commandId").GetGuid();
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
                bootId = "boot-life",
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

    private async Task IngestStateAsync(
        Guid deviceId,
        long stateSequence,
        string arm,
        long confirmedOwnershipVersion)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
        var result = await ingest.IngestAsync(new DeviceInboundEnvelope
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
                bootId = "boot-life",
                stateSequence,
                confirmedOwnershipVersion,
                armState = arm,
                sirenState = DbStatusCodes.Siren.Off,
                healthState = DbStatusCodes.Diagnostic.Unknown,
                currentAlarmEventUid = (Guid?)null,
                clockTrusted = false
            })
        });
        Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, result.Outcome);
    }

    private async Task<Guid> RegisterActivateAndOnlineAsync(HttpClient adminClient, string hardwareUid, string displayName)
    {
        var deviceId = await RegisterDeviceAsync(adminClient, hardwareUid, displayName);
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

    private static async Task<Guid> RegisterDeviceAsync(HttpClient adminClient, string hardwareUid, string displayName)
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
        return (await AuthApiFactory.ReadDataAsync(response)).GetProperty("id").GetGuid();
    }
}
