using Application.Dtos.MqttDtos;

namespace Application.Interfaces.Services;

/// <summary>
/// Validates and applies draft inbound MQTT STATE / HEARTBEAT / COMMAND_ACK payloads (T-04).
/// Command status transitions remain T-05.
/// </summary>
public interface IDeviceInboundTelemetryService
{
    Task<DeviceInboundIngestResultDto> IngestAsync(
        DeviceInboundEnvelope envelope,
        CancellationToken ct = default);
}
