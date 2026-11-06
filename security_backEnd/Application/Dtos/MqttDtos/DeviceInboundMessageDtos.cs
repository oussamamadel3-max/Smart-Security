namespace Application.Dtos.MqttDtos;

/// <summary>
/// Transport-authenticated device identity and topic binding for an inbound MQTT payload.
/// </summary>
public sealed class DeviceInboundEnvelope
{
    /// <summary>
    /// Device id claimed by the MQTT topic path (<c>devices/{deviceId}/...</c>).
    /// </summary>
    public Guid TopicDeviceId { get; set; }

    /// <summary>
    /// Device id established by broker authentication (username / cert CN). Must match topic and payload.
    /// </summary>
    public Guid AuthenticatedDeviceId { get; set; }

    /// <summary>
    /// Raw JSON payload body.
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// Optional MQTT topic string for diagnostics (never logged with secrets).
    /// </summary>
    public string? Topic { get; set; }
}

public sealed class DeviceInboundIngestResultDto
{
    public string Outcome { get; set; } = string.Empty;
    public string? RejectionCode { get; set; }
    public string? RejectionMessage { get; set; }
    public Guid DeviceId { get; set; }
    public string? MessageType { get; set; }
    public long? ObservedSequence { get; set; }
    public long? StoredSequence { get; set; }
    public bool StateApplied { get; set; }
    public bool LastSeenUpdated { get; set; }
    public bool OwnershipActivated { get; set; }
    public string? ArmState { get; set; }
    public string? SirenState { get; set; }
    public string? HealthState { get; set; }
    public bool ClockTrusted { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
}
