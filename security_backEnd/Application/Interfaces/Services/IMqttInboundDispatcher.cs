namespace Application.Interfaces.Services;

/// <summary>
/// Bridges transport-level MQTT deliveries into the validated ingest pipeline.
/// </summary>
public interface IMqttInboundDispatcher
{
    Task DispatchAsync(
        string topic,
        string payloadJson,
        Guid authenticatedDeviceId,
        CancellationToken ct = default);
}
