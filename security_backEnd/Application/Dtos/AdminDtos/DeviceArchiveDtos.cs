namespace Application.Dtos.AdminDtos;

/// <summary>
/// Admin response after archiving a device and revoking its access (FR-007).
/// </summary>
public sealed class ArchivedDeviceResponseDto
{
    public Guid Id { get; set; }
    public string HardwareUid { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LifecycleStatus { get; set; } = string.Empty;
    public DateTimeOffset RetiredAt { get; set; }
    public long CurrentOwnershipVersion { get; set; }
    public bool OwnershipEnded { get; set; }
    public string? ClosedOwnershipStatus { get; set; }
    public Guid? ClosedOwnershipId { get; set; }
    public bool OwnershipSyncQueued { get; set; }
    public int CredentialsRevoked { get; set; }
    public bool HasActiveCredential { get; set; }
    public bool BrokerAccessRevoked { get; set; }
}
