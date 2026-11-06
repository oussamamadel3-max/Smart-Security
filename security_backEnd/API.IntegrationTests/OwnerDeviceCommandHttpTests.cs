using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Dtos.MqttDtos;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Auth;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

[Collection(nameof(AuthHttpCollection))]
public sealed class OwnerDeviceCommandHttpTests : IAsyncLifetime
{
    private readonly RecordingMqttOutboundPublisher _publisher = new();
    private readonly AuthApiFactory _factory;
    private HttpClient _client = null!;

    public OwnerDeviceCommandHttpTests()
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
    public async Task Post_Arm_Returns_202_Pending_With_Outbox_Without_Claiming_Armed()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-cmd-arm-01", "أمر تفعيل");

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var idem = Guid.NewGuid();
        var response = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = idem
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var data = await AuthApiFactory.ReadDataAsync(response);
        Assert.Equal(DbStatusCodes.CommandStatus.Pending, data.GetProperty("status").GetString());
        Assert.Equal(DbStatusCodes.CommandType.Arm, data.GetProperty("type").GetString());
        Assert.False(data.GetProperty("actualStateChanged").GetBoolean());
        Assert.NotEqual(DbStatusCodes.Arm.Armed, data.GetProperty("confirmedArmState").GetString());
        var commandId = data.GetProperty("commandId").GetGuid();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
        Assert.Equal(DbStatusCodes.CommandStatus.Pending, command.Status);
        Assert.Equal(idem, command.IdempotencyKey);

        var outbox = await db.OutboxMessages.SingleAsync(m =>
            m.MessageType == ExecuteCommandDraft.OutboxPublishMessageType
            && m.AggregateId == commandId);
        Assert.Null(outbox.ProcessedAt);
        using (var doc = JsonDocument.Parse(outbox.Payload))
        {
            Assert.Equal(ExecuteCommandDraft.MqttMessageType, doc.RootElement.GetProperty("messageType").GetString());
            Assert.Equal(DbStatusCodes.CommandType.Arm, doc.RootElement.GetProperty("type").GetString());
            Assert.Equal(commandId, doc.RootElement.GetProperty("commandId").GetGuid());
        }

        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.NotEqual(DbStatusCodes.Arm.Armed, state.ArmState);
    }

    [Fact]
    public async Task Idempotent_Replay_Returns_Same_Command_Without_Second_Outbox()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-cmd-idem-01", "تكرار");

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var idem = Guid.NewGuid();
        var body = new { type = DbStatusCodes.CommandType.Disarm, idempotencyKey = idem };

        var first = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", body);
        var second = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", body);
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);

        var a = await AuthApiFactory.ReadDataAsync(first);
        var b = await AuthApiFactory.ReadDataAsync(second);
        Assert.Equal(a.GetProperty("commandId").GetGuid(), b.GetProperty("commandId").GetGuid());
        Assert.True(b.GetProperty("idempotentReplay").GetBoolean());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.DeviceCommands.CountAsync(c => c.DeviceId == deviceId));
        Assert.Equal(1, await db.OutboxMessages.CountAsync(m =>
            m.MessageType == ExecuteCommandDraft.OutboxPublishMessageType));
    }

    [Fact]
    public async Task Concurrent_Conflicting_Commands_Only_One_Pending()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-cmd-race-01", "سباق");

        using var owner1 = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        using var owner2 = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);

        var task1 = owner1.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        var task2 = owner2.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Disarm,
            idempotencyKey = Guid.NewGuid()
        });

        var results = await Task.WhenAll(task1, task2);
        var codes = results.Select(r => r.StatusCode).OrderBy(c => c).ToArray();
        Assert.Contains(HttpStatusCode.Accepted, codes);
        Assert.Contains(HttpStatusCode.Conflict, codes);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.DeviceCommands.CountAsync(c =>
            c.DeviceId == deviceId && c.Status == DbStatusCodes.CommandStatus.Pending));
    }

    [Fact]
    public async Task Offline_Device_Is_Rejected()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-cmd-off-01", "غير متصل");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            state.LastSeenAt = DateTimeOffset.UtcNow.AddHours(-2);
            await db.SaveChangesAsync();
        }

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var response = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ExecuteCommandDraft.RejectCodes.DeviceOffline, json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Pending_Ownership_Cannot_Control()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-cmd-pending-01", "معلق");
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            state.LastSeenAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var response = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Mute_Requires_Matching_Current_Alarm_Uid()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-cmd-mute-01", "كتم");
        var alarmUid = Guid.NewGuid();

        await IngestStateAsync(deviceId, 2, DbStatusCodes.Arm.Armed, DbStatusCodes.Siren.Alarm, 1, alarmUid);

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var mismatch = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.MuteAlarm,
            idempotencyKey = Guid.NewGuid(),
            targetAlarmEventUid = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);

        var ok = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.MuteAlarm,
            idempotencyKey = Guid.NewGuid(),
            targetAlarmEventUid = alarmUid
        });
        Assert.Equal(HttpStatusCode.Accepted, ok.StatusCode);
        var data = await AuthApiFactory.ReadDataAsync(ok);
        Assert.Equal(alarmUid, data.GetProperty("targetAlarmEventUid").GetGuid());
    }

    [Fact]
    public async Task TestSiren_Rejected_During_Alarm_StopTest_Requires_Test()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-cmd-test-01", "اختبار");
        var alarmUid = Guid.NewGuid();
        await IngestStateAsync(deviceId, 2, DbStatusCodes.Arm.Armed, DbStatusCodes.Siren.Alarm, 1, alarmUid);

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var duringAlarm = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.TestSiren,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Conflict, duringAlarm.StatusCode);

        await IngestStateAsync(deviceId, 3, DbStatusCodes.Arm.Disarmed, DbStatusCodes.Siren.Off, 1, null);
        var stopWithoutTest = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.StopTest,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Conflict, stopWithoutTest.StatusCode);

        await IngestStateAsync(deviceId, 4, DbStatusCodes.Arm.Disarmed, DbStatusCodes.Siren.Test, 1, null);
        var stop = await ownerClient.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.StopTest,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, stop.StatusCode);
    }

    [Fact]
    public async Task Foreign_Owner_Cannot_Create_Command()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-cmd-foreign-01", "عزل");
        await CreateExtraOwnerAsync("auth_owner_cmd_foreign");

        using var foreign = await CreateAuthorizedClientAsync("auth_owner_cmd_foreign");
        var response = await foreign.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        var confirm = await ownerships.ConfirmOwnershipVersionAsync(deviceId, ownershipVersion);
        Assert.True(confirm.Activated);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        state.LastSeenAt = DateTimeOffset.UtcNow;
        state.ArmState = DbStatusCodes.Arm.Disarmed;
        state.SirenState = DbStatusCodes.Siren.Off;
        await db.SaveChangesAsync();
        return deviceId;
    }

    private async Task IngestStateAsync(
        Guid deviceId,
        long stateSequence,
        string arm,
        string siren,
        long confirmedOwnershipVersion,
        Guid? currentAlarmEventUid)
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
                bootId = "boot-cmd",
                stateSequence,
                confirmedOwnershipVersion,
                armState = arm,
                sirenState = siren,
                healthState = DbStatusCodes.Diagnostic.Unknown,
                currentAlarmEventUid,
                clockTrusted = false
            })
        });
        Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, result.Outcome);
    }

    private async Task<Guid> CreateExtraOwnerAsync(string userName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var existing = await users.FindByNameAsync(userName);
        if (existing is not null)
            return existing.Id;

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            FullName = userName,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        var create = await users.CreateAsync(user, AuthApiFactory.Password);
        Assert.True(create.Succeeded);
        await users.AddToRoleAsync(user, DbStatusCodes.RoleCodes.Owner);
        return user.Id;
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
