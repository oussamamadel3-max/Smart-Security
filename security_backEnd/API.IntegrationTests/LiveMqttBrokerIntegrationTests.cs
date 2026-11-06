using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Interfaces.Services;
using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Protocol;

namespace API.IntegrationTests;

/// <summary>
/// SS-P9-020 live Mosquitto path. Uses Assert.SkipUnless so absence of a broker is not a green pass.
/// Fake / Recording publishers are intentionally out of scope here.
/// Inbound COMMAND_ACK over a live subscription adapter remains NOT VERIFIED until G1/ops wire it.
/// </summary>
[Collection(nameof(AuthHttpCollection))]
public sealed class LiveMqttBrokerIntegrationTests : IAsyncLifetime
{
    private AuthApiFactory? _factory;
    private HttpClient? _client;
    private LiveMqttOutboundPublisher? _livePublisher;
    private bool _brokerAvailable;
    private string _brokerDetail = MqttBrokerTestGate.SkipReason;

    public async Task InitializeAsync()
    {
        var probe = await MqttBrokerTestGate.ProbeAsync();
        _brokerAvailable = probe.Available;
        _brokerDetail = probe.Detail;
        if (!_brokerAvailable)
            return;

        _livePublisher = new LiveMqttOutboundPublisher(MqttBrokerTestGate.Host, MqttBrokerTestGate.Port);
        await _livePublisher.ConnectAsync();

        _factory = new AuthApiFactory(services =>
        {
            services.AddSingleton<IMqttOutboundPublisher>(_livePublisher);
        });
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateJsonClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (_livePublisher is not null)
            await _livePublisher.DisposeAsync();
    }

    [Fact]
    public async Task Live_Broker_Probe_Documents_Current_Environment()
    {
        var probe = await MqttBrokerTestGate.ProbeAsync();
        // Always runs: records reachability without claiming MQTT publish success.
        Assert.False(string.IsNullOrWhiteSpace(probe.Detail));
        if (!probe.Available)
            Assert.Contains("Live MQTT broker not reachable", probe.Detail, StringComparison.Ordinal);
        else
            Assert.Contains("TCP connect ok", probe.Detail, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task Outbox_Dispatch_Delivers_ExecuteCommand_To_Real_Broker_Subscriber()
    {
        Skip.IfNot(_brokerAvailable, _brokerDetail);
        Assert.NotNull(_factory);
        Assert.NotNull(_client);
        Assert.NotNull(_livePublisher);

        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-live-mqtt-01", "MQTT حي");
        var topic = ExecuteCommandDraft.Topics.Commands(deviceId);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscriber = new MqttClientFactory().CreateMqttClient();
        subscriber.ApplicationMessageReceivedAsync += e =>
        {
            received.TrySetResult(e.ApplicationMessage.ConvertPayloadToString());
            return Task.CompletedTask;
        };

        var subOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(MqttBrokerTestGate.Host, MqttBrokerTestGate.Port)
            .WithClientId($"be-t10-sub-{Guid.NewGuid():N}")
            .WithCleanSession()
            .Build();
        Assert.Equal(
            MqttClientConnectResultCode.Success,
            (await subscriber.ConnectAsync(subOptions)).ResultCode);
        await subscriber.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        var commandId = await CreatePendingArmAsync(deviceId);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            var claimed = await dispatch.DispatchPendingCommandsAsync();
            Assert.True(claimed >= 1);
        }

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(received.Task, completed);
        using var doc = JsonDocument.Parse(await received.Task);
        Assert.Equal(ExecuteCommandDraft.MqttMessageType, doc.RootElement.GetProperty("messageType").GetString());
        Assert.Equal(commandId, doc.RootElement.GetProperty("commandId").GetGuid());

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
            Assert.Equal(DbStatusCodes.CommandStatus.Pending, command.Status);
            Assert.NotNull(command.SentAt);
            // Broker delivery / PUBACK is not device execution evidence.
            Assert.Equal(DbStatusCodes.Arm.Disarmed, (await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId)).ArmState);
        }

        if (subscriber.IsConnected)
            await subscriber.DisconnectAsync();
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
        var data = await AuthApiFactory.ReadDataAsync(response);
        Assert.Equal(DbStatusCodes.CommandStatus.Pending, data.GetProperty("status").GetString());
        Assert.False(data.GetProperty("actualStateChanged").GetBoolean());
        return data.GetProperty("commandId").GetGuid();
    }

    private async Task<Guid> RegisterActivateAndOnlineAsync(HttpClient adminClient, string hardwareUid, string displayName)
    {
        var reg = await adminClient.PostAsJsonAsync("/api/admin/devices", new
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
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
        var deviceId = (await AuthApiFactory.ReadDataAsync(reg)).GetProperty("id").GetGuid();

        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
        var ownershipVersion = (await AuthApiFactory.ReadDataAsync(link)).GetProperty("ownershipVersion").GetInt64();

        await using var scope = _factory!.Services.CreateAsyncScope();
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
}
