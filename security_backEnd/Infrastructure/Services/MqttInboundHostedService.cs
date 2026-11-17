using Application.Options;
using Application.Services.MqttServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace Infrastructure.Services;

/// <summary>
/// Subscribes to device state, ack, and event channels when <see cref="MqttInboundOptions.Enabled"/> is true.
/// Each delivery is handed to <see cref="IMqttInboundDispatcher"/>; this host does not interpret payloads.
/// The subscriber session does not receive the publisher username, so the channel device id is taken from
/// the topic. Ingest still rejects a payload device id that differs from that topic.
/// A dispatch exception withholds PUBACK so QoS 1 can be redelivered. Validation rejections still acknowledge.
/// </summary>
public sealed class MqttInboundHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptionsMonitor<MqttInboundOptions> _options;
    private readonly ILogger<MqttInboundHostedService> _logger;

    public MqttInboundHostedService(
        IServiceScopeFactory scopes,
        IOptionsMonitor<MqttInboundOptions> options,
        ILogger<MqttInboundHostedService> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.CurrentValue;
        if (!opts.Enabled)
        {
            _logger.LogInformation(
                "MQTT inbound consumer is disabled (Mqtt:Inbound:Enabled=false). " +
                "Device telemetry ingest remains available through IDeviceInboundTelemetryService.");
            await IdleAsync(stoppingToken);
            return;
        }

        using var client = new MqttClientFactory().CreateMqttClient();
        using var disconnected = new SemaphoreSlim(0);
        client.ApplicationMessageReceivedAsync += message => OnMessageAsync(message);
        client.DisconnectedAsync += _ =>
        {
            try
            {
                disconnected.Release();
            }
            catch (ObjectDisposedException)
            {
            }

            return Task.CompletedTask;
        };

        var backoff = TimeSpan.FromSeconds(1);
        var subscribed = false;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!client.IsConnected)
                    {
                        await ConnectAsync(client, opts, stoppingToken);
                        subscribed = false;
                        backoff = TimeSpan.FromSeconds(1);
                    }

                    if (!subscribed)
                    {
                        await SubscribeAsync(client, opts, stoppingToken);
                        subscribed = true;
                        _logger.LogInformation(
                            "MQTT inbound subscribed. Host={Host} Port={Port}",
                            opts.Host,
                            opts.Port);
                    }

                    await disconnected.WaitAsync(TimeSpan.FromSeconds(2), stoppingToken);
                    if (!client.IsConnected)
                        subscribed = false;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    subscribed = false;
                    _logger.LogWarning(ex, "MQTT inbound connection attempt failed. Host={Host} Port={Port}", opts.Host, opts.Port);
                    try
                    {
                        await Task.Delay(backoff, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 5));
                }
            }
        }
        finally
        {
            if (client.IsConnected)
            {
                try
                {
                    var expiry = (uint)Math.Clamp(opts.SessionExpirySeconds, 0, 86_400);
                    var disconnect = new MqttClientDisconnectOptionsBuilder()
                        .WithSessionExpiryInterval(expiry)
                        .Build();
                    await client.DisconnectAsync(disconnect, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "MQTT inbound disconnect failed during shutdown.");
                }
            }
        }
    }

    private async Task OnMessageAsync(MqttApplicationMessageReceivedEventArgs message)
    {
        var qos1 = message.ApplicationMessage.QualityOfServiceLevel != MqttQualityOfServiceLevel.AtMostOnce;
        if (qos1)
            message.AutoAcknowledge = false;

        var topic = message.ApplicationMessage.Topic ?? string.Empty;
        var payload = message.ApplicationMessage.ConvertPayloadToString() ?? string.Empty;
        if (!MqttInboundDispatcher.TryParseTopic(topic, out var topicDeviceId, out _))
        {
            _logger.LogWarning("Rejected inbound MQTT topic with unexpected shape.");
            if (qos1)
                await message.AcknowledgeAsync(CancellationToken.None);
            return;
        }

        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<Application.Interfaces.Services.IMqttInboundDispatcher>();
            await dispatcher.DispatchAsync(topic, payload, topicDeviceId, CancellationToken.None);
            if (qos1)
                await message.AcknowledgeAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inbound MQTT dispatch failed. QoS 1 PUBACK was withheld.");
        }
    }

    private static async Task ConnectAsync(IMqttClient client, MqttInboundOptions opts, CancellationToken ct)
    {
        var clientId = string.IsNullOrWhiteSpace(opts.ClientId)
            ? "smart-security-backend-inbound"
            : opts.ClientId.Trim();
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(opts.Host, opts.Port)
            .WithClientId(clientId)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithCleanStart(false)
            .WithSessionExpiryInterval((uint)Math.Clamp(opts.SessionExpirySeconds, 0, 86_400))
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(Math.Clamp(opts.KeepAliveSeconds, 2, 300)))
            .WithTimeout(TimeSpan.FromSeconds(5));

        if (!string.IsNullOrWhiteSpace(opts.Username))
            builder.WithCredentials(opts.Username, opts.Password ?? string.Empty);
        MqttClientTransport.ApplyTls(builder, opts.UseTls, opts.CaCertificatePath, opts.Host);

        var result = await client.ConnectAsync(builder.Build(), ct);
        if (result.ResultCode != MqttClientConnectResultCode.Success)
            throw new InvalidOperationException($"MQTT inbound connect failed: {result.ResultCode}");
    }

    private static async Task SubscribeAsync(IMqttClient client, MqttInboundOptions opts, CancellationToken ct)
    {
        var filters = new[]
        {
            opts.StateTopicFilter,
            opts.AcksTopicFilter,
            opts.EventsTopicFilter
        };

        var builder = new MqttClientSubscribeOptionsBuilder();
        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter))
                continue;

            builder.WithTopicFilter(topicBuilder => topicBuilder
                .WithTopic(filter.Trim())
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce));
        }

        var subscribe = builder.Build();
        if (subscribe.TopicFilters.Count == 0)
            throw new InvalidOperationException("MQTT inbound has no topic filters.");

        var result = await client.SubscribeAsync(subscribe, ct);
        if (result.Items.Any(item => item.ResultCode is not (
            MqttClientSubscribeResultCode.GrantedQoS0
            or MqttClientSubscribeResultCode.GrantedQoS1
            or MqttClientSubscribeResultCode.GrantedQoS2)))
        {
            throw new InvalidOperationException("MQTT inbound subscribe was not granted.");
        }
    }

    private static async Task IdleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
