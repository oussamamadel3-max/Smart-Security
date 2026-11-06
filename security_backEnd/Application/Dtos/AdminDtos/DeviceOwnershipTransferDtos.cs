namespace Application.Dtos.AdminDtos;

/// <summary>
/// Admin request to transfer an open device ownership to a new owner (FR-006).
/// </summary>
public sealed class TransferDeviceRequestDto
{
    public Guid NewOwnerUserId { get; set; }
}

/// <summary>
/// Result of ownership transfer: prior ownership ended, new ACTIVE ownership, draft OWNERSHIP_SYNC queued.
/// </summary>
public sealed class TransferredDeviceResponseDto
{
    public Guid DeviceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string DeviceLifecycleStatus { get; set; } = string.Empty;
    public Guid PreviousOwnershipId { get; set; }
    public Guid PreviousOwnerUserId { get; set; }
    public string PreviousOwnershipStatus { get; set; } = string.Empty;
    public Guid OwnershipId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string OwnershipStatus { get; set; } = string.Empty;
    public long OwnershipVersion { get; set; }
    public long ConfirmedOwnershipVersion { get; set; }
    public bool AwaitingOwnershipAck { get; set; }
    public string ArmState { get; set; } = string.Empty;
    public bool CanPublishCommands { get; set; }
    public int CancelledCommands { get; set; }
    public int CancelledNotifications { get; set; }
    public bool OwnershipSyncQueued { get; set; }
    public string OwnershipSyncMessageType { get; set; } = string.Empty;
    public DateTimeOffset TransferredAt { get; set; }
}

/// <summary>
/// Result of unlinking a device without assigning a new owner (FR-006).
/// </summary>
public sealed class UnlinkedDeviceResponseDto
{
    public Guid DeviceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string DeviceLifecycleStatus { get; set; } = string.Empty;
    public Guid ClosedOwnershipId { get; set; }
    public Guid ClosedOwnerUserId { get; set; }
    public string ClosedOwnershipStatus { get; set; } = string.Empty;
    public long OwnershipVersion { get; set; }
    public long ConfirmedOwnershipVersion { get; set; }
    public int CancelledCommands { get; set; }
    public int CancelledNotifications { get; set; }
    public bool OwnershipSyncQueued { get; set; }
    public string OwnershipSyncMessageType { get; set; } = string.Empty;
    public DateTimeOffset UnlinkedAt { get; set; }
}
