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
    public bool CommandClosed { get; set; }
    public string? ArmState { get; set; }
    public string? SirenState { get; set; }
    public string? HealthState { get; set; }
    public bool ClockTrusted { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
}

/// <summary>
/// Result of ingesting draft ALARM_EVENT / EVENT_LOSS_REPORT / DEVICE_FAULT payloads (T-06).
/// </summary>
public sealed class AlarmEventIngestResultDto
{
    public string Outcome { get; set; } = string.Empty;
    public string? RejectionCode { get; set; }
    public string? RejectionMessage { get; set; }
    public Guid DeviceId { get; set; }
    public string? MessageType { get; set; }
    public Guid? AlarmEventId { get; set; }
    public Guid? EventUid { get; set; }
    public Guid? NotificationId { get; set; }
    public Guid? OutboxMessageId { get; set; }
    public Guid? EventLossReportId { get; set; }
    public Guid? DeviceFaultId { get; set; }
    public bool EventCreated { get; set; }
    public bool NotificationCreated { get; set; }
    public bool EventPersistedQueued { get; set; }
    public bool WasBuffered { get; set; }
    public bool ClockTrusted { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public DateTimeOffset? OccurredDeviceAt { get; set; }
}
