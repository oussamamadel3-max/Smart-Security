namespace Application.Dtos.OwnerDtos;

/// <summary>
/// Owner-facing device row. PENDING rows are intentionally limited (FR-035): name + wait state only.
/// ACTIVE rows expose four independent indicators + last update (FR-009 / FR-027 / FR-028 / BR-006).
/// </summary>
public sealed class OwnerDeviceListItemDto
{
    public Guid DeviceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string OwnershipStatus { get; set; } = string.Empty;
    public long OwnershipVersion { get; set; }
    public long ConfirmedOwnershipVersion { get; set; }
    public bool AwaitingOwnershipAck { get; set; }
    public bool CanControl { get; set; }
    public string LinkStatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// Present only for ACTIVE ownership after indicators are eligible to display.
    /// </summary>
    public OwnerDeviceIndicatorsDto? Indicators { get; set; }
}

/// <summary>
/// Four independent operational indicators. Connection is computed; never stored as is_online.
/// </summary>
public sealed class OwnerDeviceIndicatorsDto
{
    public string ConnectionStatus { get; set; } = string.Empty;
    public string ArmState { get; set; } = string.Empty;
    public string SirenState { get; set; } = string.Empty;
    public string HealthState { get; set; } = string.Empty;
    public string? HealthCode { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? LastStateAt { get; set; }
    public long StateSequence { get; set; }
    public bool ClockTrusted { get; set; }
    public Guid? CurrentAlarmEventUid { get; set; }
    public string? PowerSource { get; set; }
    public short? BatteryPercent { get; set; }
    public int HeartbeatTimeoutSeconds { get; set; }
}

/// <summary>
/// Owner device detail. PENDING remains limited; ACTIVE includes indicators and settings metadata.
/// </summary>
public sealed class OwnerDeviceDetailDto
{
    public Guid DeviceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string OwnershipStatus { get; set; } = string.Empty;
    public long OwnershipVersion { get; set; }
    public long ConfirmedOwnershipVersion { get; set; }
    public bool AwaitingOwnershipAck { get; set; }
    public bool CanControl { get; set; }
    public string LinkStatusMessage { get; set; } = string.Empty;
    public string DeviceLifecycleStatus { get; set; } = string.Empty;
    public OwnerDeviceIndicatorsDto? Indicators { get; set; }
}
