using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Interfaces.Services;
using Application.Services.MqttServices;
using Domain.Constants;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace API.IntegrationTests;

/// <summary>
/// Isolated TLS Mosquitto with ACL and file persistence.
/// Certificates and the broker password are created in a temp directory and are not committed.
/// Certificate validation is never forced to succeed. A test client is not ESP32 firmware.
/// </summary>
[Collection(nameof(AuthHttpCollection))]
public sealed class LiveMqttTlsReliabilityTests : IClassFixture<MqttTlsBrokerFixture>
{
    private readonly MqttTlsBrokerFixture _broker;

    public LiveMqttTlsReliabilityTests(MqttTlsBrokerFixture broker) => _broker = broker;

    [Fact]
    public void Committed_AppSettings_Keep_Tls_And_Live_Publish_Disabled()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(FindAppSettings()));
        var mqtt = doc.RootElement.GetProperty("Mqtt");
        Assert.False(mqtt.GetProperty("Inbound").GetProperty("Enabled").GetBoolean());
        Assert.False(mqtt.GetProperty("Inbound").GetProperty("UseTls").GetBoolean());
        Assert.False(mqtt.GetProperty("Outbound").GetProperty("LivePublishEnabled").GetBoolean());
        Assert.False(mqtt.GetProperty("Outbound").GetProperty("UseTls").GetBoolean());
        Assert.False(mqtt.GetProperty("Outbox").GetProperty("Enabled").GetBoolean());
    }

    [SkippableFact]
    public async Task Default_Os_Trust_Rejects_The_Test_Certificate()
    {
        Skip.IfNot(_broker.Ready, _broker.Detail);
        await _broker.EnsureRunningAsync();
        using var client = new MqttClientFactory().CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", _broker.Port)
            .WithClientId("os-trust-" + Guid.NewGuid().ToString("N")[..8])
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithTimeout(TimeSpan.FromSeconds(8))
            .WithTlsOptions(tls => tls.UseTls())
            .WithCredentials("srv", _broker.Password)
            .Build();

        var rejected = await RejectsConnectAsync(client, options);
        Assert.True(rejected);
    }

    [SkippableFact]
    public async Task Untrusted_Ca_Is_Rejected_And_Trusted_Ca_Receives_State()
    {
        Skip.IfNot(_broker.Ready, _broker.Detail);
        await _broker.EnsureRunningAsync();
        using (var bad = new MqttClientFactory().CreateMqttClient())
        {
            var rejected = await RejectsConnectAsync(bad, _broker.ClientOptions("bad-ca", "srv", _broker.OtherCaPath));
            Assert.True(rejected);
        }

        await using var factory = _broker.CreateFactory("tls-state");
        await factory.InitializeDatabaseAsync();
        using var http = factory.CreateJsonClient();
        using var admin = await LoginAsync(factory, http, AuthApiFactory.AdminUserName);
        var deviceId = await RegisterLinkedAsync(admin, "hw-tls-state", "شهادة");
        await _broker.PublishAsync("srv", MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 2, DbStatusCodes.Arm.Armed), qos1: true);

        var state = await WaitForStateAsync(factory, deviceId, s => s.StateSequence == 2, "TLS STATE was not stored.");
        Assert.Equal(DbStatusCodes.Arm.Armed, state.ArmState);
    }

    [SkippableFact]
    public async Task Acl_Still_Isolates_Devices_Over_Tls()
    {
        Skip.IfNot(_broker.Ready, _broker.Detail);
        await _broker.EnsureRunningAsync();
        const string deviceB = "22222222-2222-4222-8222-222222222222";
        using var deva = new MqttClientFactory().CreateMqttClient();
        Assert.Equal(MqttClientConnectResultCode.Success, (await deva.ConnectAsync(_broker.ClientOptions("deva-acl", "deva", _broker.CaPath))).ResultCode);
        var publish = await deva.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"devices/{deviceB}/state")
            .WithPayload("{\"messageType\":\"STATE\"}")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build());
        Assert.Equal(MqttClientPublishReasonCode.NotAuthorized, publish.ReasonCode);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        deva.ApplicationMessageReceivedAsync += e =>
        {
            received.TrySetResult(e.ApplicationMessage.ConvertPayloadToString() ?? string.Empty);
            return Task.CompletedTask;
        };
        await deva.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic($"devices/{deviceB}/commands").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());
        using var server = new MqttClientFactory().CreateMqttClient();
        Assert.Equal(MqttClientConnectResultCode.Success, (await server.ConnectAsync(_broker.ClientOptions("srv-acl", "srv", _broker.CaPath))).ResultCode);
        var published = await server.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"devices/{deviceB}/commands")
            .WithPayload("{\"messageType\":\"EXECUTE_COMMAND\"}")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build());
        Assert.True(published.ReasonCode is MqttClientPublishReasonCode.Success or MqttClientPublishReasonCode.NoMatchingSubscribers);
        var late = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        Assert.NotSame(received.Task, late);
    }

    [SkippableFact]
    public async Task Qos1_State_Survives_Consumer_Disconnect_And_Broker_Restart()
    {
        Skip.IfNot(_broker.Ready, _broker.Detail);
        await _broker.EnsureRunningAsync();
        const string clientId = "ss-tls-persist";
        Guid deviceId;
        await using (var first = _broker.CreateFactory(clientId))
        {
            await first.InitializeDatabaseAsync();
            using var http = first.CreateJsonClient();
            using var admin = await LoginAsync(first, http, AuthApiFactory.AdminUserName);
            deviceId = await RegisterLinkedAsync(admin, "hw-tls-persist", "استمرارية");
            await _broker.PublishAsync("srv", MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 1, DbStatusCodes.Arm.Disarmed), qos1: true);
            await WaitForStateAsync(first, deviceId, s => s.StateSequence == 1, "Initial TLS STATE was not stored.");
        }

        await _broker.PublishAsync("srv", MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 5, DbStatusCodes.Arm.Armed), qos1: true);
        await Task.Delay(TimeSpan.FromSeconds(2));
        var restarted = await MqttTlsBrokerFixture.RunAsync("docker", ["restart", _broker.ContainerName]);
        Assert.Equal(0, restarted.ExitCode);
        Assert.True(await MqttTlsBrokerFixture.WaitForTlsAsync(_broker.Port, _broker.CaPath, _broker.Password, TimeSpan.FromSeconds(60)));

        await using var second = _broker.CreateFactory(clientId);
        using var resumed = second.CreateJsonClient();
        var state = await WaitForStateAsync(second, deviceId, s => s.StateSequence == 5, "Queued QoS 1 STATE was not restored after restart.");
        Assert.Equal(DbStatusCodes.Arm.Armed, state.ArmState);
    }

    [SkippableFact]
    public async Task Failed_Dispatch_Is_Retried_Once_After_Broker_Restart()
    {
        Skip.IfNot(_broker.Ready, _broker.Detail);
        await _broker.EnsureRunningAsync();
        var gate = new FailOnceGate();
        await using var factory = _broker.CreateFactory("tls-fail-once", services =>
        {
            services.AddScoped<IMqttInboundDispatcher>(sp =>
                new FailOnceDispatcher(ActivatorUtilities.CreateInstance<MqttInboundDispatcher>(sp), gate));
        });
        await factory.InitializeDatabaseAsync();
        using var http = factory.CreateJsonClient();
        using var admin = await LoginAsync(factory, http, AuthApiFactory.AdminUserName);
        var deviceId = await RegisterLinkedAsync(admin, "hw-tls-retry", "إعادة");
        await _broker.PublishAsync("srv", MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 3, DbStatusCodes.Arm.Armed), qos1: true);

        var sawFailure = await WaitUntilAsync(() => gate.Failures >= 1, TimeSpan.FromSeconds(15));
        Assert.True(sawFailure);
        await Task.Delay(TimeSpan.FromSeconds(2));
        var restarted = await MqttTlsBrokerFixture.RunAsync("docker", ["restart", _broker.ContainerName]);
        Assert.Equal(0, restarted.ExitCode);
        Assert.True(await MqttTlsBrokerFixture.WaitForTlsAsync(_broker.Port, _broker.CaPath, _broker.Password, TimeSpan.FromSeconds(60)));

        var state = await WaitForStateAsync(factory, deviceId, s => s.StateSequence == 3, "Withheld QoS 1 STATE was not redelivered.");
        Assert.Equal(DbStatusCodes.Arm.Armed, state.ArmState);
        Assert.Equal(1, gate.Successes);
    }

    [SkippableFact]
    public async Task Outbox_Worker_Retries_When_Broker_Returns_And_Leaves_Command_Pending()
    {
        Skip.IfNot(_broker.Ready, _broker.Detail);
        await _broker.EnsureRunningAsync();
        await using var factory = _broker.CreateFactory("tls-outbox", outbox: true, retryDelaySeconds: 25);
        await factory.InitializeDatabaseAsync();
        using var http = factory.CreateJsonClient();
        using var admin = await LoginAsync(factory, http, AuthApiFactory.AdminUserName);
        var deviceId = await RegisterLinkedAsync(admin, "hw-tls-outbox", "صندوق");
        await _broker.PublishAsync("srv", MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 1, DbStatusCodes.Arm.Disarmed), qos1: true);
        await WaitForStateAsync(factory, deviceId, s => s.StateSequence == 1, "Activation STATE was not stored.");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var settings = await db.DeviceSettings.SingleAsync(s => s.DeviceId == deviceId);
            settings.CommandTimeoutSeconds = 300;
            await db.SaveChangesAsync();
        }

        var topic = ExecuteCommandDraft.Topics.Commands(deviceId);
        var received = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        var stopped = await MqttTlsBrokerFixture.RunAsync("docker", ["stop", _broker.ContainerName]);
        Assert.Equal(0, stopped.ExitCode);
        var down = false;
        var downDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < downDeadline)
        {
            if (!await MqttTlsBrokerFixture.WaitForTlsAsync(_broker.Port, _broker.CaPath, _broker.Password, TimeSpan.FromSeconds(2)))
            {
                down = true;
                break;
            }
        }

        Assert.True(down);
        Guid commandId;
        try
        {
            using var owner = await LoginAsync(factory, http, AuthApiFactory.OwnerUserName);
            var response = await owner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
            {
                type = DbStatusCodes.CommandType.Arm,
                idempotencyKey = Guid.NewGuid()
            });
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            commandId = (await AuthApiFactory.ReadDataAsync(response)).GetProperty("commandId").GetGuid();
            var failed = await WaitUntilAsync(async () =>
            {
                await using var scope = factory.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var row = await db.OutboxMessages.AsNoTracking().SingleOrDefaultAsync(o => o.AggregateId == commandId);
                return row is { AttemptCount: > 0, ProcessedAt: null };
            }, TimeSpan.FromSeconds(40));
            Assert.True(failed);
        }
        finally
        {
            await MqttTlsBrokerFixture.RunAsync("docker", ["start", _broker.ContainerName]);
            Assert.True(await MqttTlsBrokerFixture.WaitForTlsAsync(_broker.Port, _broker.CaPath, _broker.Password, TimeSpan.FromSeconds(60)));
        }

        using var subscriber = new MqttClientFactory().CreateMqttClient();
        subscriber.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == topic)
                received.TrySetResult(e.ApplicationMessage);
            return Task.CompletedTask;
        };
        Assert.Equal(MqttClientConnectResultCode.Success, (await subscriber.ConnectAsync(_broker.ClientOptions("sub-out", "srv", _broker.CaPath))).ResultCode);
        await subscriber.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(40)));
        Assert.Same(received.Task, completed);
        var message = await received.Task;
        Assert.False(message.Retain);
        Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, message.QualityOfServiceLevel);

        await using var verify = factory.Services.CreateAsyncScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var command = await dbVerify.DeviceCommands.SingleAsync(c => c.Id == commandId);
        Assert.Equal(DbStatusCodes.CommandStatus.Pending, command.Status);
        Assert.Equal(DbStatusCodes.Arm.Disarmed, (await dbVerify.DeviceStates.SingleAsync(s => s.DeviceId == deviceId)).ArmState);
    }

    [SkippableFact]
    public async Task Expired_Command_Is_Not_Published_Or_Executed()
    {
        Skip.IfNot(_broker.Ready, _broker.Detail);
        await _broker.EnsureRunningAsync();
        await using var factory = _broker.CreateFactory("tls-expired");
        await factory.InitializeDatabaseAsync();
        using var http = factory.CreateJsonClient();
        using var admin = await LoginAsync(factory, http, AuthApiFactory.AdminUserName);
        var deviceId = await RegisterLinkedAsync(admin, "hw-tls-exp", "منته");
        await _broker.PublishAsync("srv", MqttInboundDraft.Topics.State(deviceId), StatePayload(deviceId, 1, DbStatusCodes.Arm.Disarmed), qos1: true);
        await WaitForStateAsync(factory, deviceId, s => s.StateSequence == 1, "Activation STATE was not stored.");

        var topic = ExecuteCommandDraft.Topics.Commands(deviceId);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscriber = new MqttClientFactory().CreateMqttClient();
        subscriber.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == topic)
                received.TrySetResult();
            return Task.CompletedTask;
        };
        await subscriber.ConnectAsync(_broker.ClientOptions("sub-exp", "srv", _broker.CaPath));
        await subscriber.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());

        using var owner = await LoginAsync(factory, http, AuthApiFactory.OwnerUserName);
        var response = await owner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var commandId = (await AuthApiFactory.ReadDataAsync(response)).GetProperty("commandId").GetGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
            command.CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-30);
            command.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-5);
            await db.SaveChangesAsync();
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingCommandsAsync();
        }

        var late = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        Assert.NotSame(received.Task, late);
        await using var verify = factory.Services.CreateAsyncScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await dbVerify.DeviceCommands.SingleAsync(c => c.Id == commandId);
        Assert.NotEqual(DbStatusCodes.CommandStatus.Executed, stored.Status);
        var outbox = await dbVerify.OutboxMessages.SingleAsync(o => o.AggregateId == commandId);
        Assert.Equal(ExecuteCommandDraft.RejectCodes.CommandExpired, outbox.LastErrorCode);
        Assert.NotNull(outbox.ProcessedAt);
    }

    private static async Task<bool> RejectsConnectAsync(IMqttClient client, MqttClientOptions options)
    {
        try
        {
            var result = await client.ConnectAsync(options);
            return result.ResultCode != MqttClientConnectResultCode.Success;
        }
        catch (Exception ex) when (ex is MqttCommunicationException or IOException or System.Security.Authentication.AuthenticationException)
        {
            return true;
        }
    }

    private static async Task<HttpClient> LoginAsync(AuthApiFactory factory, HttpClient anonymous, string userName)
    {
        var login = await anonymous.PostAsJsonAsync("/api/auth/login", new
        {
            userName,
            password = AuthApiFactory.Password,
            refreshDelivery = "body"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var access = (await AuthApiFactory.ReadDataAsync(login)).GetProperty("accessToken").GetString()!;
        var client = factory.CreateJsonClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        return client;
    }

    private static async Task<Guid> RegisterLinkedAsync(HttpClient admin, string hardwareUid, string displayName)
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

    private static async Task<Domain.Entities.Devices.DeviceState> WaitForStateAsync(
        AuthApiFactory factory,
        Guid deviceId,
        Func<Domain.Entities.Devices.DeviceState, bool> done,
        string failure)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(40);
        while (DateTime.UtcNow < deadline)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var state = await db.DeviceStates.AsNoTracking().SingleOrDefaultAsync(s => s.DeviceId == deviceId);
            if (state is not null && done(state))
                return state;
            await Task.Delay(250);
        }

        throw new TimeoutException(failure);
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> done, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (done())
                return true;
            await Task.Delay(200);
        }

        return false;
    }

    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> done, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await done())
                return true;
            await Task.Delay(400);
        }

        return false;
    }

    private static string StatePayload(Guid deviceId, long sequence, string arm) => JsonSerializer.Serialize(new
    {
        schemaVersion = 1,
        deviceId,
        messageId = Guid.NewGuid(),
        messageType = MqttInboundDraft.MessageTypes.State,
        bootId = "boot-tls",
        stateSequence = sequence,
        confirmedOwnershipVersion = 1,
        armState = arm,
        sirenState = DbStatusCodes.Siren.Off,
        healthState = DbStatusCodes.Diagnostic.Unknown,
        currentAlarmEventUid = (Guid?)null,
        clockTrusted = false
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
}

public sealed class FailOnceGate
{
    private int _failures;
    private int _successes;
    public int Failures => _failures;
    public int Successes => _successes;
    public bool TryFail() => Interlocked.Increment(ref _failures) == 1;
    public void MarkSuccess() => Interlocked.Increment(ref _successes);
}

public sealed class FailOnceDispatcher : IMqttInboundDispatcher
{
    private readonly IMqttInboundDispatcher _inner;
    private readonly FailOnceGate _gate;

    public FailOnceDispatcher(IMqttInboundDispatcher inner, FailOnceGate gate)
    {
        _inner = inner;
        _gate = gate;
    }

    public async Task DispatchAsync(string topic, string payloadJson, Guid authenticatedDeviceId, CancellationToken ct = default)
    {
        if (_gate.TryFail())
            throw new IOException("Injected dispatch failure before the database transaction.");
        await _inner.DispatchAsync(topic, payloadJson, authenticatedDeviceId, ct);
        _gate.MarkSuccess();
    }
}

public sealed class MqttTlsBrokerFixture : IAsyncLifetime
{
    private string? _tempDir;
    public int Port { get; private set; }
    public string Password { get; private set; } = string.Empty;
    public string CaPath { get; private set; } = string.Empty;
    public string OtherCaPath { get; private set; } = string.Empty;
    public string ContainerName { get; private set; } = string.Empty;
    public bool Ready { get; private set; }
    public string Detail { get; private set; } = "Docker was not probed.";

    public async Task InitializeAsync()
    {
        try
        {
            var docker = await RunAsync("docker", ["info"]);
            if (docker.ExitCode != 0)
            {
                Detail = "Docker is not available. TLS Mosquitto was not started.";
                return;
            }
        }
        catch (Win32Exception ex)
        {
            Detail = "Docker executable was not found. " + ex.Message;
            return;
        }

        _tempDir = Path.Combine(Path.GetTempPath(), "ss-g1-tls-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        CaPath = Path.Combine(_tempDir, "ca.crt");
        OtherCaPath = Path.Combine(_tempDir, "other-ca.crt");
        WriteCertificates(_tempDir, CaPath, OtherCaPath);
        File.Copy(FindAcl(), Path.Combine(_tempDir, "acl"));
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "mosquitto.conf"),
            """
            listener 8883 0.0.0.0
            cafile /tmp/ca.crt
            certfile /tmp/server.crt
            keyfile /tmp/server.key
            require_certificate false
            allow_anonymous false
            password_file /tmp/passwd
            acl_file /tmp/acl
            persistence true
            persistence_location /tmp/persist/
            autosave_interval 1
            log_type error
            log_type warning
            """);

        Port = ReserveLoopbackPort();
        ContainerName = "ss-g1-tls-" + Guid.NewGuid().ToString("N")[..12];
        var script =
            "mkdir -p /tmp/persist && " +
            "cp /mosquitto/config/ca.crt /tmp/ca.crt && " +
            "cp /mosquitto/config/server.crt /tmp/server.crt && " +
            "cp /mosquitto/config/server.key /tmp/server.key && " +
            "cp /mosquitto/config/acl /tmp/acl && " +
            "chown mosquitto:mosquitto /tmp/server.key /tmp/persist && " +
            "chmod 640 /tmp/server.key && " +
            "chmod 750 /tmp/persist && " +
            "rm -f /tmp/passwd && " +
            "mosquitto_passwd -b -c /tmp/passwd srv \"$MQTT_PASSWORD\" && " +
            "mosquitto_passwd -b /tmp/passwd deva \"$MQTT_PASSWORD\" && " +
            "mosquitto_passwd -b /tmp/passwd devb \"$MQTT_PASSWORD\" && " +
            "chown mosquitto:mosquitto /tmp/passwd /tmp/acl && " +
            "chmod 640 /tmp/passwd && " +
            "exec mosquitto -c /mosquitto/config/mosquitto.conf";
        var started = await RunAsync("docker",
        [
            "run", "-d", "--name", ContainerName,
            "--entrypoint", "sh",
            "-p", $"127.0.0.1:{Port}:8883",
            "-e", "MQTT_PASSWORD=" + Password,
            "-v", _tempDir + ":/mosquitto/config:ro",
            "eclipse-mosquitto:2",
            "-c", script
        ]);
        if (started.ExitCode != 0)
        {
            Detail = "docker run failed. " + Trim(started.StdErr + started.StdOut);
            return;
        }

        Ready = await WaitForTlsAsync(Port, CaPath, Password, TimeSpan.FromSeconds(40));
        Detail = Ready
            ? $"TLS Mosquitto accepted a trusted-CA handshake on 127.0.0.1:{Port}. Private keys stayed in the temp directory."
            : "TLS Mosquitto did not complete a trusted handshake. " + Trim(await LogsAsync(ContainerName));
    }

    public async Task DisposeAsync()
    {
        if (!string.IsNullOrEmpty(ContainerName))
        {
            try
            {
                await RunAsync("docker", ["rm", "-f", ContainerName]);
            }
            catch (Win32Exception)
            {
            }
        }

        if (_tempDir is not null && Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    public AuthApiFactory CreateFactory(
        string clientSuffix,
        Action<IServiceCollection>? configure = null,
        bool outbox = false,
        int retryDelaySeconds = 1)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Mqtt:Inbound:Enabled"] = "true",
            ["Mqtt:Inbound:Host"] = "127.0.0.1",
            ["Mqtt:Inbound:Port"] = Port.ToString(),
            ["Mqtt:Inbound:ClientId"] = "ss-in-" + clientSuffix,
            ["Mqtt:Inbound:Username"] = "srv",
            ["Mqtt:Inbound:Password"] = Password,
            ["Mqtt:Inbound:UseTls"] = "true",
            ["Mqtt:Inbound:CaCertificatePath"] = CaPath,
            ["Mqtt:Inbound:KeepAliveSeconds"] = "2",
            ["Mqtt:Inbound:SessionExpirySeconds"] = "3600",
            ["Mqtt:Outbound:LivePublishEnabled"] = "true",
            ["Mqtt:Outbound:Host"] = "127.0.0.1",
            ["Mqtt:Outbound:Port"] = Port.ToString(),
            ["Mqtt:Outbound:ClientId"] = "ss-out-" + clientSuffix,
            ["Mqtt:Outbound:Username"] = "srv",
            ["Mqtt:Outbound:Password"] = Password,
            ["Mqtt:Outbound:UseTls"] = "true",
            ["Mqtt:Outbound:CaCertificatePath"] = CaPath,
            ["Mqtt:Outbox:Enabled"] = outbox ? "true" : "false",
            ["Mqtt:Outbox:PollIntervalMs"] = "400",
            ["Mqtt:Outbox:RetryDelaySeconds"] = Math.Max(1, retryDelaySeconds).ToString(),
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
            ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
        };
        return new AuthApiFactory(configure, settings);
    }

    public MqttClientOptions ClientOptions(string clientPrefix, string user, string caPath)
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", Port)
            .WithClientId(clientPrefix + "-" + Guid.NewGuid().ToString("N")[..8])
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithTimeout(TimeSpan.FromSeconds(8))
            .WithCleanStart()
            .WithCredentials(user, Password)
            .WithTlsOptions(tls =>
            {
                tls.UseTls();
                tls.WithCertificateValidationHandler(args =>
                    MqttServerCertificateCheck.IsTrusted(args.Certificate, caPath, "127.0.0.1"));
            })
            .Build();
    }

    public async Task EnsureRunningAsync()
    {
        if (await WaitForTlsAsync(Port, CaPath, Password, TimeSpan.FromSeconds(3)))
            return;
        await RunAsync("docker", ["start", ContainerName]);
        if (!await WaitForTlsAsync(Port, CaPath, Password, TimeSpan.FromSeconds(60)))
            throw new InvalidOperationException("TLS Mosquitto did not return.");
    }

    public async Task PublishAsync(string user, string topic, string payload, bool qos1)
    {
        using var client = new MqttClientFactory().CreateMqttClient();
        var connected = await client.ConnectAsync(ClientOptions("pub", user, CaPath));
        Assert.Equal(MqttClientConnectResultCode.Success, connected.ResultCode);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        MqttClientPublishResult result;
        do
        {
            result = await client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qos1 ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce)
                .WithRetainFlag(false)
                .Build());
            if (result.ReasonCode == MqttClientPublishReasonCode.Success)
                return;
            if (result.ReasonCode != MqttClientPublishReasonCode.NoMatchingSubscribers)
                break;
            await Task.Delay(300);
        }
        while (DateTime.UtcNow < deadline);

        Assert.Equal(MqttClientPublishReasonCode.Success, result.ReasonCode);
    }

    public static async Task<bool> WaitForTlsAsync(int port, string caPath, string password, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new MqttClientFactory().CreateMqttClient();
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("127.0.0.1", port)
                    .WithClientId("probe-" + Guid.NewGuid().ToString("N")[..8])
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithTimeout(TimeSpan.FromSeconds(8))
                    .WithCredentials("srv", password)
                    .WithTlsOptions(tls =>
                    {
                        tls.UseTls();
                        tls.WithCertificateValidationHandler(args =>
                            MqttServerCertificateCheck.IsTrusted(args.Certificate, caPath, "127.0.0.1"));
                    })
                    .Build();
                var result = await client.ConnectAsync(options);
                if (result.ResultCode == MqttClientConnectResultCode.Success)
                {
                    await client.DisconnectAsync();
                    return true;
                }
            }
            catch (Exception ex) when (ex is MqttCommunicationException or SocketException or IOException or System.Security.Authentication.AuthenticationException)
            {
            }

            await Task.Delay(500);
        }

        return false;
    }

    private static void WriteCertificates(string directory, string caPath, string otherCaPath)
    {
        using var caKey = RSA.Create(2048);
        var caRequest = new CertificateRequest("CN=ss-g1-test-ca", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(caRequest.PublicKey, false));
        var notBefore = DateTimeOffset.UtcNow.AddHours(-1);
        var caNotAfter = DateTimeOffset.UtcNow.AddDays(3);
        using var ca = caRequest.CreateSelfSigned(notBefore, caNotAfter);

        using var serverKey = RSA.Create(2048);
        var serverRequest = new CertificateRequest("CN=127.0.0.1", serverKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddIpAddress(IPAddress.Parse("127.0.0.1"));
        san.AddDnsName("localhost");
        serverRequest.CertificateExtensions.Add(san.Build());
        serverRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        serverRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        using var serverPublic = serverRequest.Create(ca, notBefore, caNotAfter.AddHours(-1), RandomNumberGenerator.GetBytes(16));
        using var server = serverPublic.CopyWithPrivateKey(serverKey);

        File.WriteAllText(caPath, ca.ExportCertificatePem());
        File.WriteAllText(Path.Combine(directory, "server.crt"), server.ExportCertificatePem());
        File.WriteAllText(Path.Combine(directory, "server.key"), serverKey.ExportPkcs8PrivateKeyPem());

        using var otherKey = RSA.Create(2048);
        var otherRequest = new CertificateRequest("CN=ss-g1-other-ca", otherKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        otherRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        using var other = otherRequest.CreateSelfSigned(notBefore, caNotAfter);
        File.WriteAllText(otherCaPath, other.ExportCertificatePem());
    }

    private static string FindAcl()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "security_backEnd", "API.IntegrationTests", "mosquitto", "acl");
            if (File.Exists(candidate))
                return candidate;
            var nearby = Path.Combine(dir.FullName, "mosquitto", "acl");
            if (File.Exists(nearby))
                return nearby;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Mosquitto ACL fixture was not found.");
    }

    private static int ReserveLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<string> LogsAsync(string containerName)
    {
        var logs = await RunAsync("docker", ["logs", "--tail", "40", containerName]);
        return logs.StdErr + logs.StdOut;
    }

    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(string file, IReadOnlyList<string> args)
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
        if (compact.Contains("MQTT_PASSWORD", StringComparison.Ordinal))
            compact = "redacted";
        return compact.Length <= 500 ? compact : compact[..500];
    }
}
