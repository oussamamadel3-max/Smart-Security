using Application.Interfaces.Services;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace Infrastructure.Services;

/// <summary>
/// Outbound Mosquitto publisher used only when <see cref="MqttLivePublishOptions.LivePublishEnabled"/> is true.
/// Publishes at QoS 1 without the retain flag. A dropped connection is reopened on the next publish.
/// </summary>
public sealed class MqttNetLivePublisher : IMqttOutboundPublisher, IAsyncDisposable
{
    private readonly IOptions<MqttLivePublishOptions> _options;
    private readonly ILogger<MqttNetLivePublisher> _logger;
    private readonly IMqttClient _client;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public MqttNetLivePublisher(
        IOptions<MqttLivePublishOptions> options,
        ILogger<MqttNetLivePublisher> logger)
    {
        _options = options;
        _logger = logger;
        _client = new MqttClientFactory().CreateMqttClient();
    }

    public bool IsLivePublishEnabled => true;

    public async Task PublishAsync(string topic, string payloadJson, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            await EnsureConnectedAsync(timeout.Token);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payloadJson)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            var result = await _client.PublishAsync(message, timeout.Token);
            if (result.ReasonCode != MqttClientPublishReasonCode.Success
                && result.ReasonCode != MqttClientPublishReasonCode.NoMatchingSubscribers)
            {
                throw new InvalidOperationException($"MQTT publish failed: {result.ReasonCode}");
            }
        }
        catch
        {
            if (_client.IsConnected)
            {
                try
                {
                    await _client.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "MQTT outbound disconnect after a failed publish did not complete.");
                }
            }

            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_client.IsConnected)
                await _client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "MQTT outbound disconnect failed during shutdown.");
        }

        _client.Dispose();
        _gate.Dispose();
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_client.IsConnected)
            return;

        var opts = _options.Value;
        var clientId = string.IsNullOrWhiteSpace(opts.ClientId)
            ? "smart-security-backend-outbound"
            : opts.ClientId.Trim();
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(opts.Host, opts.Port)
            .WithClientId(clientId)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithCleanStart()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(Math.Clamp(opts.KeepAliveSeconds, 2, 300)))
            .WithTimeout(TimeSpan.FromSeconds(5));

        if (!string.IsNullOrWhiteSpace(opts.Username))
            builder.WithCredentials(opts.Username, opts.Password ?? string.Empty);
        MqttClientTransport.ApplyTls(builder, opts.UseTls, opts.CaCertificatePath, opts.Host);

        var result = await _client.ConnectAsync(builder.Build(), ct);
        if (result.ResultCode != MqttClientConnectResultCode.Success)
            throw new InvalidOperationException($"MQTT outbound connect failed: {result.ResultCode}");

        _logger.LogInformation(
            "MQTT outbound connected. Host={Host} Port={Port} ClientId={ClientId}",
            opts.Host,
            opts.Port,
            clientId);
    }
}
