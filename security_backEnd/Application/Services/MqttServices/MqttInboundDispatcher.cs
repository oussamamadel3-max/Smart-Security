using System.Text.RegularExpressions;
using Application.Dtos.MqttDtos;
using Application.Interfaces.Services;
using Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Application.Services.MqttServices;

public sealed class MqttInboundDispatcher : IMqttInboundDispatcher
{
    private static readonly Regex TopicDeviceRegex = new(
        @"^devices/(?<deviceId>[0-9a-fA-F-]{36})/(?<channel>state|acks|events)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IDeviceInboundTelemetryService _telemetry;
    private readonly IAlarmEventIngestService _alarms;
    private readonly ILogger<MqttInboundDispatcher> _logger;

    public MqttInboundDispatcher(
        IDeviceInboundTelemetryService telemetry,
        IAlarmEventIngestService alarms,
        ILogger<MqttInboundDispatcher> logger)
    {
        _telemetry = telemetry;
        _alarms = alarms;
        _logger = logger;
    }

    public async Task DispatchAsync(
        string topic,
        string payloadJson,
        Guid authenticatedDeviceId,
        CancellationToken ct = default)
    {
        if (!TryParseTopic(topic, out var topicDeviceId, out var channel))
        {
            _logger.LogWarning("Rejected inbound MQTT topic with unexpected shape.");
            return;
        }

        if (string.Equals(channel, "events", StringComparison.Ordinal))
        {
            var alarmResult = await _alarms.IngestAsync(
                new DeviceInboundEnvelope
                {
                    Topic = topic,
                    TopicDeviceId = topicDeviceId,
                    AuthenticatedDeviceId = authenticatedDeviceId,
                    PayloadJson = payloadJson
                },
                ct);

            if (alarmResult.Outcome == AlarmEventDraft.IngestOutcomes.Rejected)
            {
                _logger.LogInformation(
                    "Inbound MQTT event rejected for device {DeviceId}: {Code}",
                    alarmResult.DeviceId,
                    alarmResult.RejectionCode);
                return;
            }

            _logger.LogDebug(
                "Inbound MQTT {MessageType} for device {DeviceId}: {Outcome}",
                alarmResult.MessageType,
                alarmResult.DeviceId,
                alarmResult.Outcome);
            return;
        }

        var result = await _telemetry.IngestAsync(
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
        => TryParseTopic(topic, out deviceId, out _);

    public static bool TryParseTopic(string topic, out Guid deviceId, out string channel)
    {
        deviceId = Guid.Empty;
        channel = string.Empty;
        if (string.IsNullOrWhiteSpace(topic))
            return false;

        var match = TopicDeviceRegex.Match(topic.Trim());
        if (!match.Success)
            return false;

        if (!Guid.TryParse(match.Groups["deviceId"].Value, out deviceId))
            return false;

        channel = match.Groups["channel"].Value;
        return true;
    }
}
