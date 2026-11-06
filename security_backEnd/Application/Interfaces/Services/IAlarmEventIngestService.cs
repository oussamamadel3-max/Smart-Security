using Application.Dtos.MqttDtos;

namespace Application.Interfaces.Services;

/// <summary>
/// Validates and persists draft inbound ALARM_EVENT / EVENT_LOSS_REPORT / DEVICE_FAULT (T-06).
/// EVENT_PERSISTED is queued in the same DB transaction and published only after commit via outbox.
/// </summary>
public interface IAlarmEventIngestService
{
    Task<AlarmEventIngestResultDto> IngestAsync(
        DeviceInboundEnvelope envelope,
        CancellationToken ct = default);
}
