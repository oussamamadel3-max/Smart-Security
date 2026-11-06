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
public sealed class DeviceInboundTelemetryTests : IAsyncLifetime
{
    private readonly AuthApiFactory _factory = new();
    private HttpClient _client = null!;

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
    public async Task State_Applies_When_Sequence_Advances_And_Activates_Pending_Ownership()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-mqtt-state-01", "جهاز حالة");
        await LinkAsync(adminClient, deviceId);

        var result = await IngestStateAsync(deviceId, stateSequence: 1, arm: DbStatusCodes.Arm.Armed, confirmedOwnershipVersion: 1);

        Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, result.Outcome);
        Assert.True(result.StateApplied);
        Assert.True(result.OwnershipActivated);
        Assert.Equal(DbStatusCodes.Arm.Armed, result.ArmState);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(1, state.StateSequence);
        Assert.Equal(DbStatusCodes.Arm.Armed, state.ArmState);
        Assert.Equal(DbStatusCodes.Siren.Off, state.SirenState);
        Assert.Equal(DbStatusCodes.Diagnostic.Unknown, state.HealthState);
        Assert.False(state.ClockTrusted);
        Assert.NotNull(state.LastSeenAt);
        Assert.NotNull(state.LastStateAt);

        var ownership = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Ownership.Active, ownership.Status);
    }

    [Fact]
    public async Task Older_Sequence_Does_Not_Overwrite_Newer_State_But_Refreshes_LastSeen()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-mqtt-stale-01", "جهاز قديم");
        await LinkAsync(adminClient, deviceId);

        await IngestStateAsync(deviceId, stateSequence: 10, arm: DbStatusCodes.Arm.Armed, confirmedOwnershipVersion: 1);
        DateTimeOffset? lastSeenAfterNew;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            lastSeenAfterNew = (await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId)).LastSeenAt;
        }

        await Task.Delay(20);
        var stale = await IngestStateAsync(
            deviceId,
            stateSequence: 8,
            arm: DbStatusCodes.Arm.Disarmed,
            confirmedOwnershipVersion: 1,
            health: DbStatusCodes.Diagnostic.Healthy);

        Assert.Equal(MqttInboundDraft.IngestOutcomes.IgnoredStaleSequence, stale.Outcome);
        Assert.False(stale.StateApplied);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db2.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(10, state.StateSequence);
        Assert.Equal(DbStatusCodes.Arm.Armed, state.ArmState);
        Assert.Equal(DbStatusCodes.Diagnostic.Unknown, state.HealthState);
        Assert.True(state.LastSeenAt > lastSeenAfterNew);
    }

    [Fact]
    public async Task Foreign_Authenticated_Device_Cannot_Update_Target()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var targetId = await RegisterDeviceAsync(adminClient, "hw-uid-mqtt-target", "هدف");
        var attackerId = await RegisterDeviceAsync(adminClient, "hw-uid-mqtt-attacker", "مهاجم");
        await LinkAsync(adminClient, targetId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
        var payload = BuildStatePayload(targetId, 5, DbStatusCodes.Arm.Armed, 1);
        var result = await ingest.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = targetId,
            AuthenticatedDeviceId = attackerId,
            PayloadJson = payload,
            Topic = MqttInboundDraft.Topics.State(targetId)
        });

        Assert.Equal(MqttInboundDraft.IngestOutcomes.Rejected, result.Outcome);
        Assert.Equal("DEVICE_IDENTITY_MISMATCH", result.RejectionCode);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == targetId);
        Assert.Equal(0, state.StateSequence);
        Assert.Equal(DbStatusCodes.Arm.Unknown, state.ArmState);
    }

    [Fact]
    public async Task Payload_DeviceId_Mismatch_Is_Rejected()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-mqtt-payload-mismatch", "عدم تطابق");
        var otherId = await RegisterDeviceAsync(adminClient, "hw-uid-mqtt-other", "آخر");

        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
        var payload = BuildStatePayload(otherId, 3, DbStatusCodes.Arm.Armed, 1);
        var result = await ingest.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = deviceId,
            AuthenticatedDeviceId = deviceId,
            PayloadJson = payload,
            Topic = MqttInboundDraft.Topics.State(deviceId)
        });

        Assert.Equal(MqttInboundDraft.IngestOutcomes.Rejected, result.Outcome);
        Assert.Equal("DEVICE_IDENTITY_MISMATCH", result.RejectionCode);
    }

    [Fact]
    public async Task Heartbeat_Updates_LastSeen_Only()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-mqtt-hb", "نبضة");
        await IngestStateAsync(deviceId, stateSequence: 2, arm: DbStatusCodes.Arm.Disarmed, confirmedOwnershipVersion: 0);

        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
        var payload = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            deviceId,
            messageId = Guid.NewGuid(),
            messageType = MqttInboundDraft.MessageTypes.Heartbeat,
            bootId = "boot-hb",
            clockTrusted = false
        });

        var result = await ingest.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = deviceId,
            AuthenticatedDeviceId = deviceId,
            PayloadJson = payload,
            Topic = MqttInboundDraft.Topics.State(deviceId)
        });

        Assert.Equal(MqttInboundDraft.IngestOutcomes.HeartbeatOnly, result.Outcome);
        Assert.True(result.LastSeenUpdated);
        Assert.False(result.StateApplied);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(2, state.StateSequence);
        Assert.Equal(DbStatusCodes.Arm.Disarmed, state.ArmState);
        Assert.NotNull(state.LastSeenAt);
    }

    [Fact]
    public async Task CommandAck_Applies_Actual_State_Using_ResultStateSequence_Without_Rewinding()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-mqtt-ack", "تأكيد");
        await IngestStateAsync(deviceId, stateSequence: 10, arm: DbStatusCodes.Arm.Armed, confirmedOwnershipVersion: 0);

        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();

        var lateAck = await ingest.IngestAsync(new DeviceInboundEnvelope
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
                bootId = "boot-ack",
                commandId = Guid.NewGuid(),
                ownershipVersion = 1,
                status = DbStatusCodes.CommandStatus.Executed,
                resultStateSequence = 7,
                armState = DbStatusCodes.Arm.Disarmed,
                sirenState = DbStatusCodes.Siren.Off,
                healthState = DbStatusCodes.Diagnostic.Unknown,
                currentAlarmEventUid = (Guid?)null
            })
        });
        Assert.Equal(MqttInboundDraft.IngestOutcomes.IgnoredStaleSequence, lateAck.Outcome);

        var newerAck = await ingest.IngestAsync(new DeviceInboundEnvelope
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
                bootId = "boot-ack-2",
                commandId = Guid.NewGuid(),
                ownershipVersion = 1,
                status = DbStatusCodes.CommandStatus.Executed,
                resultStateSequence = 11,
                armState = DbStatusCodes.Arm.Disarmed,
                sirenState = DbStatusCodes.Siren.Off,
                healthState = DbStatusCodes.Diagnostic.Unknown,
                currentAlarmEventUid = (Guid?)null,
                clockTrusted = false
            })
        });
        Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, newerAck.Outcome);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(11, state.StateSequence);
        Assert.Equal(DbStatusCodes.Arm.Disarmed, state.ArmState);
    }

    [Fact]
    public async Task Concurrent_State_Updates_Keep_Highest_Sequence_On_PostgreSQL()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-mqtt-race", "سباق");
        await IngestStateAsync(deviceId, stateSequence: 5, arm: DbStatusCodes.Arm.Unknown, confirmedOwnershipVersion: 0);

        // Warm the lock path once so schema/plan is ready, then race many writers.
        var payloads = Enumerable.Range(0, 20).Select(i =>
        {
            var seq = 6 + (i % 10); // sequences 6..15 mixed with duplicates and disorder
            var arm = seq % 2 == 0 ? DbStatusCodes.Arm.Armed : DbStatusCodes.Arm.Disarmed;
            return (Sequence: (long)seq, Arm: arm, Payload: BuildStatePayload(deviceId, seq, arm, 0));
        }).ToList();

        var tasks = payloads.Select(async item =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var ingest = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
            return await ingest.IngestAsync(new DeviceInboundEnvelope
            {
                TopicDeviceId = deviceId,
                AuthenticatedDeviceId = deviceId,
                PayloadJson = item.Payload,
                Topic = MqttInboundDraft.Topics.State(deviceId)
            });
        });

        var results = await Task.WhenAll(tasks);
        Assert.Contains(results, r => r.Outcome == MqttInboundDraft.IngestOutcomes.Applied);
        Assert.All(results, r => Assert.NotEqual(MqttInboundDraft.IngestOutcomes.Rejected, r.Outcome));

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(15, state.StateSequence);
        // Highest sequence (15) is odd => DISARMED in the mapping above.
        Assert.Equal(DbStatusCodes.Arm.Disarmed, state.ArmState);
        Assert.NotEqual(DbStatusCodes.Diagnostic.Healthy, state.HealthState);
    }

    [Fact]
    public async Task Retired_Device_Inbound_Is_Rejected()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-mqtt-retired", "مؤرشف");
        var archive = await adminClient.PostAsync($"/api/admin/devices/{deviceId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);

        var result = await IngestStateAsync(deviceId, 1, DbStatusCodes.Arm.Armed, 0);
        Assert.Equal(MqttInboundDraft.IngestOutcomes.Rejected, result.Outcome);
        Assert.Equal("DEVICE_RETIRED", result.RejectionCode);
    }

    private async Task<DeviceInboundIngestResultDto> IngestStateAsync(
        Guid deviceId,
        long stateSequence,
        string arm,
        long confirmedOwnershipVersion,
        string health = DbStatusCodes.Diagnostic.Unknown)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
        return await ingest.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = deviceId,
            AuthenticatedDeviceId = deviceId,
            Topic = MqttInboundDraft.Topics.State(deviceId),
            PayloadJson = BuildStatePayload(deviceId, stateSequence, arm, confirmedOwnershipVersion, health)
        });
    }

    private static string BuildStatePayload(
        Guid deviceId,
        long stateSequence,
        string arm,
        long confirmedOwnershipVersion,
        string health = DbStatusCodes.Diagnostic.Unknown)
        => JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            deviceId,
            messageId = Guid.NewGuid(),
            messageType = MqttInboundDraft.MessageTypes.State,
            bootId = "boot-example",
            stateSequence,
            confirmedOwnershipVersion,
            armState = arm,
            sirenState = DbStatusCodes.Siren.Off,
            healthState = health,
            currentAlarmEventUid = (Guid?)null,
            clockTrusted = false
        });

    private static async Task LinkAsync(HttpClient adminClient, Guid deviceId)
    {
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
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
