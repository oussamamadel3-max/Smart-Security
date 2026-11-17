using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Devices;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace API.IntegrationTests;

/// <summary>
/// Backend hosted consumer and live outbox publisher against a disposable Mosquitto.
/// These tests do not call ingest services directly and do not use RecordingMqttOutboundPublisher.
/// A passing subscriber is not ESP32 firmware evidence. TLS is not claimed.
/// </summary>
[Collection(nameof(AuthHttpCollection))]
public sealed class LiveMqttInboundIntegrationTests : IAsyncLifetime
{
    private string? _containerName;
    private string? _tempDir;
    private int _port;
    private bool _brokerReady;
    private string _brokerDetail = "Docker was not probed.";
    private AuthApiFactory? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        try
        {
            var docker = await RunAsync("docker", ["info"]);
            if (docker.ExitCode != 0)
            {
                _brokerDetail = "Docker is not available. Live inbound MQTT was not started. " + Trim(docker.StdErr);
                return;
            }
        }
        catch (Win32Exception ex)
        {
            _brokerDetail = "Docker executable was not found, so live inbound MQTT was not started. " + ex.Message;
            return;
        }

        _tempDir = Path.Combine(Path.GetTempPath(), "ss-g1-in-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "mosquitto.conf"),
            "listener 1883 0.0.0.0\nallow_anonymous true\npersistence false\nlog_type error\nlog_type warning\n");

        _port = ReserveLoopbackPort();
        _containerName = "ss-g1-in-" + Guid.NewGuid().ToString("N")[..12];
        var started = await RunAsync("docker",
        [
            "run", "-d", "--name", _containerName,
            "-p", $"127.0.0.1:{_port}:1883",
            "-v", _tempDir + ":/mosquitto/config:ro",
            "eclipse-mosquitto:2"
        ]);
        if (started.ExitCode != 0)
        {
            _brokerDetail = "docker run failed. " + Trim(started.StdErr + started.StdOut);
            return;
        }

        _brokerReady = await WaitForTcpAsync(_port, TimeSpan.FromSeconds(30));
        _brokerDetail = _brokerReady
            ? $"Anonymous Mosquitto accepted TCP on 127.0.0.1:{_port}. No TLS listener was started."
            : "Anonymous Mosquitto did not accept TCP.";
        if (!_brokerReady)
            return;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        _factory = new AuthApiFactory(settings: new Dictionary<string, string?>
        {
            ["Mqtt:Inbound:Enabled"] = "true",
            ["Mqtt:Inbound:Host"] = "127.0.0.1",
            ["Mqtt:Inbound:Port"] = _port.ToString(),
            ["Mqtt:Inbound:ClientId"] = "ss-in-" + suffix,
            ["Mqtt:Inbound:KeepAliveSeconds"] = "2",
            ["Mqtt:Outbound:LivePublishEnabled"] = "true",
            ["Mqtt:Outbound:Host"] = "127.0.0.1",
            ["Mqtt:Outbound:Port"] = _port.ToString(),
            ["Mqtt:Outbound:ClientId"] = "ss-out-" + suffix,
            ["Mqtt:Outbound:KeepAliveSeconds"] = "2",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
            ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
        });
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateJsonClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (_containerName is not null)
        {
            try
            {
                await RunAsync("docker", ["rm", "-f", _containerName]);
            }
            catch (Win32Exception)
            {
            }
        }

        if (_tempDir is not null && Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Committed_AppSettings_Keep_Live_Mqtt_Disabled()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(FindAppSettings()));
        var mqtt = doc.RootElement.GetProperty("Mqtt");
        Assert.False(mqtt.GetProperty("Inbound").GetProperty("Enabled").GetBoolean());
        Assert.False(mqtt.GetProperty("Outbound").GetProperty("LivePublishEnabled").GetBoolean());
        Assert.False(mqtt.GetProperty("Outbox").GetProperty("Enabled").GetBoolean());
    }

    [SkippableFact]
    public async Task Http_Command_Reaches_Subscriber_At_Qos1_Without_Retain()
    {
        Skip.IfNot(_brokerReady, _brokerDetail);
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterAndActivateAsync(admin, "hw-live-in-cmd", "أمر حي");
        var topic = ExecuteCommandDraft.Topics.Commands(deviceId);

        var received = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscriber = await ConnectTestClientAsync("sub-cmd");
        subscriber.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == topic)
                received.TrySetResult(e.ApplicationMessage);
            return Task.CompletedTask;
        };
        await subscriber.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        var commandId = await CreatePendingArmAsync(deviceId);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            Assert.True(await dispatch.DispatchPendingCommandsAsync() >= 1);
        }

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.Same(received.Task, completed);
        var message = await received.Task;
        Assert.False(message.Retain);
        Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, message.QualityOfServiceLevel);
        using var payload = JsonDocument.Parse(message.ConvertPayloadToString());
        Assert.Equal(ExecuteCommandDraft.MqttMessageType, payload.RootElement.GetProperty("messageType").GetString());
        Assert.Equal(commandId, payload.RootElement.GetProperty("commandId").GetGuid());

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
        Assert.Equal(DbStatusCodes.CommandStatus.Pending, command.Status);
        Assert.Equal(DbStatusCodes.Arm.Disarmed, (await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId)).ArmState);
    }

    [SkippableFact]
    public async Task State_Heartbeat_And_Payload_Mismatch_Go_Through_The_Hosted_Consumer()
    {
        Skip.IfNot(_brokerReady, _brokerDetail);
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterLinkedAsync(admin, "hw-live-in-state", "حالة");
        var otherId = await RegisterLinkedAsync(admin, "hw-live-in-other", "آخر");

        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), StatePayload(otherId, 4, DbStatusCodes.Arm.Armed), qos1: true);
        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 2, DbStatusCodes.Arm.Armed), qos1: true);
        var state = await WaitForStateAsync(deviceId, s => s.StateSequence == 2, "STATE was not applied through the hosted consumer.");
        Assert.Equal(DbStatusCodes.Arm.Armed, state.ArmState);

        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), HeartbeatPayload(deviceId), qos1: false);
        var afterHeartbeat = await WaitForStateAsync(
            deviceId,
            s => s.StateSequence == 2 && s.LastSeenAt > state.LastSeenAt,
            "HEARTBEAT did not refresh last_seen through the hosted consumer.");
        Assert.Equal(DbStatusCodes.Arm.Armed, afterHeartbeat.ArmState);

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var other = await db.DeviceStates.SingleAsync(s => s.DeviceId == otherId);
        Assert.Equal(0, other.StateSequence);
        Assert.Equal(DbStatusCodes.Arm.Unknown, other.ArmState);
    }

    [SkippableFact]
    public async Task Ack_Order_Duplicate_And_Stale_State_Do_Not_Rewind_Confirmed_State()
    {
        Skip.IfNot(_brokerReady, _brokerDetail);
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterAndActivateAsync(admin, "hw-live-in-order", "ترتيب");

        var first = await CreatePendingArmAsync(deviceId);
        await PublishAsync(
            MqttInboundDraft.Topics.Acks(deviceId),
            AckPayload(deviceId, first, 6, DbStatusCodes.Arm.Armed),
            qos1: true);
        await WaitForStateAsync(deviceId, s => s.StateSequence == 6 && s.ArmState == DbStatusCodes.Arm.Armed, "ACK before STATE was not applied.");
        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 7, DbStatusCodes.Arm.Armed), qos1: true);
        var armed = await WaitForStateAsync(deviceId, s => s.StateSequence == 7, "STATE after ACK was not applied.");
        Assert.Equal(DbStatusCodes.Arm.Armed, armed.ArmState);

        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 7, DbStatusCodes.Arm.Disarmed), qos1: true);
        var duplicate = await WaitForStateAsync(
            deviceId,
            s => s.StateSequence == 7 && s.LastSeenAt > armed.LastSeenAt,
            "Duplicate STATE did not reach the hosted consumer.");
        Assert.Equal(DbStatusCodes.Arm.Armed, duplicate.ArmState);

        var second = await CreatePendingArmAsync(deviceId);
        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 9, DbStatusCodes.Arm.Disarmed), qos1: true);
        await WaitForStateAsync(deviceId, s => s.StateSequence == 9, "Newer STATE was not applied.");
        await PublishAsync(
            MqttInboundDraft.Topics.Acks(deviceId),
            AckPayload(deviceId, second, 8, DbStatusCodes.Arm.Armed),
            qos1: true);
        await WaitForCommandAsync(second, DbStatusCodes.CommandStatus.Executed, "ACK after newer STATE did not close the command.");

        var newest = await WaitForStateAsync(deviceId, s => s.StateSequence == 9, "Newer STATE was not applied.");
        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 4, DbStatusCodes.Arm.Armed), qos1: true);
        var stale = await WaitForStateAsync(
            deviceId,
            s => s.StateSequence == 9 && s.LastSeenAt > newest.LastSeenAt,
            "Stale STATE did not reach the hosted consumer.");
        Assert.Equal(DbStatusCodes.Arm.Disarmed, stale.ArmState);

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(9, state.StateSequence);
        Assert.Equal(DbStatusCodes.Arm.Disarmed, state.ArmState);
        Assert.Equal(DbStatusCodes.CommandStatus.Executed, (await db.DeviceCommands.SingleAsync(c => c.Id == first)).Status);
    }

    [SkippableFact]
    public async Task Late_Ack_After_Timeout_Closes_Command_Without_Rewinding_State()
    {
        Skip.IfNot(_brokerReady, _brokerDetail);
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterAndActivateAsync(admin, "hw-live-in-late", "متأخر");
        var commandId = await CreatePendingArmAsync(deviceId);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            Assert.True(await dispatch.DispatchPendingCommandsAsync() >= 1);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
            command.SentAt = DateTimeOffset.UtcNow.AddSeconds(-30);
            command.CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-30);
            await db.SaveChangesAsync();
            Assert.True(await dispatch.MarkTimedOutCommandsAsync() >= 1);
        }

        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 10, DbStatusCodes.Arm.Disarmed), qos1: true);
        await WaitForStateAsync(deviceId, s => s.StateSequence == 10, "STATE before the late ACK was not stored.");
        await PublishAsync(
            MqttInboundDraft.Topics.Acks(deviceId),
            AckPayload(deviceId, commandId, 4, DbStatusCodes.Arm.Armed),
            qos1: true);
        await WaitForCommandAsync(commandId, DbStatusCodes.CommandStatus.Executed, "Late ACK was not applied through the hosted consumer.");

        await using var verify = _factory.Services.CreateAsyncScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await dbVerify.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(10, state.StateSequence);
        Assert.Equal(DbStatusCodes.Arm.Disarmed, state.ArmState);
        Assert.True(await dbVerify.CommandStatusHistory.AnyAsync(h => h.CommandId == commandId && h.IsLateAck));
    }

    [SkippableFact]
    public async Task Alarm_Loss_And_Fault_Are_Stored_And_EventPersisted_Is_Published()
    {
        Skip.IfNot(_brokerReady, _brokerDetail);
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterAndActivateAsync(admin, "hw-live-in-alarm", "حادث");
        var eventUid = Guid.NewGuid();
        var reportUid = Guid.NewGuid();
        var faultUid = Guid.NewGuid();
        var commands = ExecuteCommandDraft.Topics.Commands(deviceId);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscriber = await ConnectTestClientAsync("sub-persisted");
        subscriber.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == commands)
                received.TrySetResult(e.ApplicationMessage.ConvertPayloadToString() ?? string.Empty);
            return Task.CompletedTask;
        };
        await subscriber.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic(commands)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        var events = AlarmEventDraft.Topics.Events(deviceId);
        await PublishAsync(events, AlarmPayload(deviceId, eventUid), qos1: true);
        await PublishAsync(events, AlarmPayload(deviceId, eventUid), qos1: true);
        await PublishAsync(events, LossPayload(deviceId, reportUid), qos1: true);
        await PublishAsync(events, FaultPayload(deviceId, faultUid), qos1: true);

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var alarmReady = await db.AlarmEvents.AnyAsync(e => e.DeviceId == deviceId && e.EventUid == eventUid);
            var lossReady = await db.EventLossReports.AnyAsync(r => r.DeviceId == deviceId && r.ReportUid == reportUid);
            var faultReady = await db.DeviceFaults.AnyAsync(f => f.DeviceId == deviceId && f.FaultUid == faultUid);
            if (alarmReady && lossReady && faultReady)
                break;
            await Task.Delay(200);
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            Assert.True(await dispatch.DispatchPendingEventPersistedAsync() >= 1);
        }

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.Same(received.Task, completed);
        using var persisted = JsonDocument.Parse(await received.Task);
        Assert.Equal(EventPersistedDraft.MqttMessageType, persisted.RootElement.GetProperty("messageType").GetString());
        Assert.Equal(eventUid, persisted.RootElement.GetProperty("eventUid").GetGuid());
        Assert.True(persisted.RootElement.GetProperty("stored").GetBoolean());

        await Task.Delay(TimeSpan.FromSeconds(1));
        await using var verify = _factory.Services.CreateAsyncScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await dbVerify.AlarmEvents.CountAsync(e => e.DeviceId == deviceId && e.EventUid == eventUid));
        Assert.Equal(1, await dbVerify.EventLossReports.CountAsync(r => r.DeviceId == deviceId && r.ReportUid == reportUid));
        Assert.Equal(1, await dbVerify.DeviceFaults.CountAsync(f => f.DeviceId == deviceId && f.FaultUid == faultUid));
    }

    [SkippableFact]
    public async Task Consumer_Resubscribes_After_Broker_Restart()
    {
        Skip.IfNot(_brokerReady, _brokerDetail);
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterLinkedAsync(admin, "hw-live-in-re", "إعادة اتصال");
        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 2, DbStatusCodes.Arm.Armed), qos1: true);
        await WaitForStateAsync(deviceId, s => s.StateSequence == 2, "STATE before the broker restart was not stored.");

        var restarted = await RunAsync("docker", ["restart", _containerName!]);
        Assert.Equal(0, restarted.ExitCode);
        var listening = await WaitForTcpAsync(_port, TimeSpan.FromSeconds(60));
        if (!listening)
        {
            var logs = await RunAsync("docker", ["logs", "--tail", "30", _containerName!]);
            Assert.Fail("Broker did not accept TCP on 127.0.0.1:" + _port + " after restart. " + Trim(logs.StdErr + logs.StdOut));
        }

        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 3, DbStatusCodes.Arm.Disarmed), qos1: true);
        var state = await WaitForStateAsync(deviceId, s => s.StateSequence == 3, "STATE after broker restart was not stored.");
        Assert.Equal(DbStatusCodes.Arm.Disarmed, state.ArmState);
    }

    private async Task<Guid> RegisterAndActivateAsync(HttpClient admin, string hardwareUid, string displayName)
    {
        var deviceId = await RegisterLinkedAsync(admin, hardwareUid, displayName);
        await PublishAsync(MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 1, DbStatusCodes.Arm.Disarmed), qos1: true);
        await WaitForStateAsync(deviceId, s => s.StateSequence == 1, "Activation STATE was not stored.");
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ownership = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Ownership.Active, ownership.Status);
        return deviceId;
    }

    private async Task<Guid> RegisterLinkedAsync(HttpClient admin, string hardwareUid, string displayName)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/devices", new
        {
            hardwareUid,
            displayName,
            credential = new { authType = DbStatusCodes.AuthType.PasswordHash, password = "MqttSecret!99" },
            settings = new { eventBufferCapacity = 64, commandTtlSeconds = 120, alarmRule = new { schemaVersion = 1 } }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var deviceId = (await AuthApiFactory.ReadDataAsync(response)).GetProperty("id").GetGuid();
        var link = await admin.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new { ownerUserId = AuthApiFactory.OwnerId });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
        return deviceId;
    }

    private async Task<Guid> CreatePendingArmAsync(Guid deviceId)
    {
        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var response = await owner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var data = await AuthApiFactory.ReadDataAsync(response);
        Assert.Equal(DbStatusCodes.CommandStatus.Pending, data.GetProperty("status").GetString());
        Assert.False(data.GetProperty("actualStateChanged").GetBoolean());
        return data.GetProperty("commandId").GetGuid();
    }

    private async Task PublishAsync(string topic, string payload, bool qos1)
    {
        using var client = await ConnectTestClientAsync("pub");
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos1 ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce)
            .WithRetainFlag(false)
            .Build();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        MqttClientPublishResult result;
        do
        {
            result = await client.PublishAsync(message);
            if (result.ReasonCode == MqttClientPublishReasonCode.Success)
                return;
            if (result.ReasonCode != MqttClientPublishReasonCode.NoMatchingSubscribers)
                break;
            await Task.Delay(300);
        }
        while (DateTime.UtcNow < deadline);

        Assert.Equal(MqttClientPublishReasonCode.Success, result.ReasonCode);
    }

    private async Task<IMqttClient> ConnectTestClientAsync(string prefix)
    {
        var client = new MqttClientFactory().CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", _port)
            .WithClientId(prefix + "-" + Guid.NewGuid().ToString("N")[..8])
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithCleanStart()
            .Build();
        var result = await client.ConnectAsync(options);
        Assert.Equal(MqttClientConnectResultCode.Success, result.ResultCode);
        return client;
    }

    private async Task<DeviceState> WaitForStateAsync(Guid deviceId, Func<DeviceState, bool> done, string failure)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(25);
        while (DateTime.UtcNow < deadline)
        {
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var state = await db.DeviceStates.AsNoTracking().SingleAsync(s => s.DeviceId == deviceId);
            if (done(state))
                return state;
            await Task.Delay(200);
        }

        throw new TimeoutException(failure);
    }

    private async Task WaitForCommandAsync(Guid commandId, string status, string failure)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(25);
        while (DateTime.UtcNow < deadline)
        {
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var command = await db.DeviceCommands.AsNoTracking().SingleAsync(c => c.Id == commandId);
            if (command.Status == status)
                return;
            await Task.Delay(200);
        }

        throw new TimeoutException(failure);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(string userName)
    {
        var login = await _client!.PostAsJsonAsync("/api/auth/login", new
        {
            userName,
            password = AuthApiFactory.Password,
            refreshDelivery = "body"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var access = (await AuthApiFactory.ReadDataAsync(login)).GetProperty("accessToken").GetString()!;
        var client = _factory!.CreateJsonClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        return client;
    }

    private static string StatePayload(Guid deviceId, long sequence, string arm) => JsonSerializer.Serialize(new
    {
        schemaVersion = 1,
        deviceId,
        messageId = Guid.NewGuid(),
        messageType = MqttInboundDraft.MessageTypes.State,
        bootId = "boot-live",
        stateSequence = sequence,
        confirmedOwnershipVersion = 1,
        armState = arm,
        sirenState = DbStatusCodes.Siren.Off,
        healthState = DbStatusCodes.Diagnostic.Unknown,
        currentAlarmEventUid = (Guid?)null,
        clockTrusted = false
    });

    private static string HeartbeatPayload(Guid deviceId) => JsonSerializer.Serialize(new
    {
        schemaVersion = 1,
        deviceId,
        messageId = Guid.NewGuid(),
        messageType = MqttInboundDraft.MessageTypes.Heartbeat,
        bootId = "boot-live",
        clockTrusted = false
    });

    private static string AckPayload(Guid deviceId, Guid commandId, long sequence, string arm) => JsonSerializer.Serialize(new
    {
        schemaVersion = 1,
        deviceId,
        messageId = Guid.NewGuid(),
        messageType = MqttInboundDraft.MessageTypes.CommandAck,
        bootId = "boot-live",
        commandId,
        ownershipVersion = 1,
        status = DbStatusCodes.CommandStatus.Executed,
        resultStateSequence = sequence,
        armState = arm,
        sirenState = DbStatusCodes.Siren.Off,
        healthState = DbStatusCodes.Diagnostic.Unknown,
        currentAlarmEventUid = (Guid?)null,
        clockTrusted = false
    });

    private static string AlarmPayload(Guid deviceId, Guid eventUid) => JsonSerializer.Serialize(new
    {
        schemaVersion = 1,
        deviceId,
        messageId = Guid.NewGuid(),
        messageType = AlarmEventDraft.MessageTypes.AlarmEvent,
        bootId = "boot-live",
        eventUid,
        ownershipVersion = 1,
        sensorKey = "vibration-1",
        wasBuffered = false,
        clockTrusted = false,
        occurredDeviceAt = (DateTimeOffset?)null
    });

    private static string LossPayload(Guid deviceId, Guid reportUid) => JsonSerializer.Serialize(new
    {
        schemaVersion = 1,
        deviceId,
        messageId = Guid.NewGuid(),
        messageType = AlarmEventDraft.MessageTypes.EventLossReport,
        bootId = "boot-live",
        reportUid,
        ownershipVersion = 1,
        reason = DbStatusCodes.EventLossReason.BufferFull,
        lostCount = 2,
        clockTrusted = false,
        deviceOccurredAt = (DateTimeOffset?)null
    });

    private static string FaultPayload(Guid deviceId, Guid faultUid) => JsonSerializer.Serialize(new
    {
        schemaVersion = 1,
        deviceId,
        messageId = Guid.NewGuid(),
        messageType = AlarmEventDraft.MessageTypes.DeviceFault,
        bootId = "boot-live",
        faultUid,
        ownershipVersion = 1,
        faultCode = "SENSOR_READ_FAULT",
        status = DbStatusCodes.Fault.Active,
        clockTrusted = false,
        deviceOccurredAt = (DateTimeOffset?)null
    });

    private static string FindAppSettings()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "security_backEnd", "API", "appsettings.json");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("appsettings.json was not found.");
    }

    private static int ReserveLoopbackPort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<(bool Published, int Port, string Detail)> WaitForPublishedPortAsync(string containerName)
    {
        var last = "";
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var mapped = await RunAsync("docker", ["port", containerName, "1883/tcp"]);
            last = Trim(mapped.StdErr + mapped.StdOut);
            if (mapped.ExitCode == 0 && TryParsePort(mapped.StdOut, out var port))
                return (true, port, last);
            await Task.Delay(500);
        }

        return (false, 0, last);
    }

    private static bool TryParsePort(string text, out int port)
    {
        port = 0;
        var line = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (line is null)
            return false;
        var colon = line.LastIndexOf(':');
        return colon >= 0 && int.TryParse(line[(colon + 1)..].Trim(), out port);
    }

    private static async Task<bool> WaitForTcpAsync(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync("127.0.0.1", port);
                return true;
            }
            catch (SocketException)
            {
                await Task.Delay(500);
            }
        }

        return false;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(string file, IReadOnlyList<string> args)
    {
        var start = new ProcessStartInfo
        {
            FileName = file,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
            start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Failed to start " + file);
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, stdout, stderr);
    }

    private static string Trim(string text)
    {
        var compact = text.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        return compact.Length <= 400 ? compact : compact[..400];
    }
}
