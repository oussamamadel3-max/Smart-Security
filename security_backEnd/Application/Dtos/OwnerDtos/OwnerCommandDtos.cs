namespace Application.Dtos.OwnerDtos;

public sealed class CreateOwnerDeviceCommandRequestDto
{
    /// <summary>
    /// ARM | DISARM | MUTE_ALARM | TEST_SIREN | STOP_TEST.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Client idempotency key; required for POST command (HTTP contract §4.2).
    /// </summary>
    public Guid IdempotencyKey { get; set; }

    /// <summary>
    /// Required for MUTE_ALARM — must match the device-reported current alarm event uid.
    /// </summary>
    public Guid? TargetAlarmEventUid { get; set; }
}

public sealed class OwnerDeviceCommandResponseDto
{
    public Guid CommandId { get; set; }
    public Guid DeviceId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long CommandSequence { get; set; }
    public long OwnershipVersion { get; set; }
    public long ObservedStateSequence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid? TargetAlarmEventUid { get; set; }
    public string? RejectCode { get; set; }
    public long? ResultStateSequence { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public DateTimeOffset? ReconciledAt { get; set; }
    public bool IdempotentReplay { get; set; }

    /// <summary>
    /// Always false on HTTP accept — actual arm/siren come only from device-confirmed state.
    /// </summary>
    public bool ActualStateChanged { get; set; }

    public string? ConfirmedArmState { get; set; }
    public string? ConfirmedSirenState { get; set; }
}
