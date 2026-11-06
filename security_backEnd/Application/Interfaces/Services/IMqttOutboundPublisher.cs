namespace Application.Interfaces.Services;

/// <summary>
/// Draft outbound MQTT publisher. Production Mosquitto adapter stays behind G1/ops.
/// </summary>
public interface IMqttOutboundPublisher
{
    /// <summary>
    /// True when a real broker publish path is configured and allowed.
    /// </summary>
    bool IsLivePublishEnabled { get; }

    /// <summary>
    /// Publishes EXECUTE_COMMAND JSON to the device commands topic (QoS 1, not retained).
    /// Must be idempotent with respect to commandId — retries may deliver duplicates.
    /// </summary>
    Task PublishAsync(
        string topic,
        string payloadJson,
        CancellationToken ct = default);
}
