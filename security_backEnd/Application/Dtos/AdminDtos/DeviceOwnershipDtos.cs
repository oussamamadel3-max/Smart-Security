namespace Application.Dtos.AdminDtos;

/// <summary>
/// Admin request to create the initial PENDING ownership for a registered device (FR-004).
/// </summary>
public sealed class LinkDeviceRequestDto
{
    public Guid OwnerUserId { get; set; }
}

/// <summary>
/// Result of an initial link: PENDING ownership + queued draft OWNERSHIP_SYNC outbox row.
/// </summary>
public sealed class LinkedDeviceResponseDto
{
    public Guid DeviceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string DeviceLifecycleStatus { get; set; } = string.Empty;
    public Guid OwnershipId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string OwnershipStatus { get; set; } = string.Empty;
    public long OwnershipVersion { get; set; }
    public long ConfirmedOwnershipVersion { get; set; }
    public string ArmState { get; set; } = string.Empty;
    public bool CanControl { get; set; }
    public bool OwnershipSyncQueued { get; set; }
    public string OwnershipSyncMessageType { get; set; } = string.Empty;
    public DateTimeOffset LinkedAt { get; set; }
}
