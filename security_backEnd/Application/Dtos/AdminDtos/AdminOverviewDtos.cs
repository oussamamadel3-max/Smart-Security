namespace Application.Dtos.AdminDtos;

/// <summary>
/// Admin owner account row (FR-008). No secrets.
/// </summary>
public sealed class AdminOwnerListItemDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Admin device overview with computed connectivity and last known confirmed state (FR-008 / FR-027).
/// Offline is never equated with DISARMED or FAULT.
/// </summary>
public sealed class AdminDeviceOverviewDto
{
    public Guid DeviceId { get; set; }
    public string HardwareUid { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LifecycleStatus { get; set; } = string.Empty;
    public long CurrentOwnershipVersion { get; set; }

    public Guid? OwnershipId { get; set; }
    public string? OwnershipStatus { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? OwnerUserName { get; set; }
    public string? OwnerFullName { get; set; }
    public long? OwnershipVersion { get; set; }
    public long ConfirmedOwnershipVersion { get; set; }
    public bool AwaitingOwnershipAck { get; set; }

    /// <summary>
    /// Always present for admin: computed ONLINE/OFFLINE from last_seen_at + timeout.
    /// </summary>
    public AdminDeviceIndicatorsDto Indicators { get; set; } = new();
}

/// <summary>
/// Four independent indicators for admin. Health stays UNKNOWN until device reports diagnosis.
/// </summary>
public sealed class AdminDeviceIndicatorsDto
{
    public string ConnectionStatus { get; set; } = string.Empty;
    public string ArmState { get; set; } = string.Empty;
    public string SirenState { get; set; } = string.Empty;
    public string HealthState { get; set; } = string.Empty;
    public string? HealthCode { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>
    /// Time of last accepted STATE/ACK that updated arm/siren/health (last known state clock).
    /// </summary>
    public DateTimeOffset? LastStateAt { get; set; }

    public long StateSequence { get; set; }
    public bool ClockTrusted { get; set; }
    public Guid? CurrentAlarmEventUid { get; set; }
    public string? PowerSource { get; set; }
    public short? BatteryPercent { get; set; }
    public int HeartbeatTimeoutSeconds { get; set; }
}
