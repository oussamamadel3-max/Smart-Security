using System.Text.RegularExpressions;
using Application.Dtos.MqttDtos;
using Application.Interfaces.Services;
using Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Application.Services.MqttServices;

public sealed class MqttInboundDispatcher : IMqttInboundDispatcher
{
    private static readonly Regex TopicDeviceRegex = new(
        @"^devices/(?<deviceId>[0-9a-fA-F-]{36})/(state|acks)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IDeviceInboundTelemetryService _ingest;
    private readonly ILogger<MqttInboundDispatcher> _logger;

    public MqttInboundDispatcher(
        IDeviceInboundTelemetryService ingest,
        ILogger<MqttInboundDispatcher> logger)
    {
        _ingest = ingest;
        _logger = logger;
    }

    public async Task DispatchAsync(
        string topic,
        string payloadJson,
        Guid authenticatedDeviceId,
        CancellationToken ct = default)
    {
        if (!TryParseTopicDeviceId(topic, out var topicDeviceId))
        {
            _logger.LogWarning("Rejected inbound MQTT topic with unexpected shape.");
            return;
        }

        var result = await _ingest.IngestAsync(
            new DeviceInboundEnvelope
            {
                Topic = topic,
                TopicDeviceId = topicDeviceId,
                AuthenticatedDeviceId = authenticatedDeviceId,
                PayloadJson = payloadJson
            },
            ct);

        if (result.Outcome == MqttInboundDraft.IngestOutcomes.Rejected)
        {
            _logger.LogInformation(
                "Inbound MQTT rejected for device {DeviceId}: {Code}",
                result.DeviceId,
                result.RejectionCode);
            return;
        }

        _logger.LogDebug(
            "Inbound MQTT {MessageType} for device {DeviceId}: {Outcome}",
            result.MessageType,
            result.DeviceId,
            result.Outcome);
    }

    public static bool TryParseTopicDeviceId(string topic, out Guid deviceId)
    {
        deviceId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(topic))
            return false;

        var match = TopicDeviceRegex.Match(topic.Trim());
        if (!match.Success)
            return false;

        return Guid.TryParse(match.Groups["deviceId"].Value, out deviceId);
    }
}
