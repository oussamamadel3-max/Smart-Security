using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Default outbound publisher until G1/ops register a live Mosquitto adapter.
/// Does not claim transport success; IsLivePublishEnabled stays false.
/// </summary>
public sealed class NoOpMqttOutboundPublisher : IMqttOutboundPublisher
{
    private readonly ILogger<NoOpMqttOutboundPublisher> _logger;

    public NoOpMqttOutboundPublisher(ILogger<NoOpMqttOutboundPublisher> logger)
    {
        _logger = logger;
    }

    public bool IsLivePublishEnabled => false;

    public Task PublishAsync(string topic, string payloadJson, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "No-Op MQTT outbound publish skipped (G1/ops gate). Topic={Topic} PayloadBytes={Bytes}",
            topic,
            payloadJson?.Length ?? 0);
        return Task.CompletedTask;
    }
}
