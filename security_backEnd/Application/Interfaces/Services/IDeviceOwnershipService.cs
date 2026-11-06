using Application.Dtos.AdminDtos;

namespace Application.Interfaces.Services;

/// <summary>
/// Initial PENDING link, ownership-version acknowledgment, transfer, and unlink (FR-004 / FR-006 / FR-035).
/// Confirmation is also applied by the MQTT STATE ingest path; tests may call it via DI.
/// </summary>
public interface IDeviceOwnershipService
{
    Task<LinkedDeviceResponseDto> LinkInitialAsync(
        Guid deviceId,
        LinkDeviceRequestDto request,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Applies a device-reported confirmedOwnershipVersion (draft STATE semantics).
    /// Activates open PENDING ownership only when the confirmed version matches that row.
    /// </summary>
    Task<OwnershipConfirmationResultDto> ConfirmOwnershipVersionAsync(
        Guid deviceId,
        long confirmedOwnershipVersion,
        CancellationToken ct = default);

    /// <summary>
    /// Ends the open ownership and opens a new ACTIVE ownership for the new owner in one transaction.
    /// Cancels prior-owner pending commands/notifications and queues draft OWNERSHIP_SYNC.
    /// </summary>
    Task<TransferredDeviceResponseDto> TransferAsync(
        Guid deviceId,
        TransferDeviceRequestDto request,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Ends the open ownership without assigning a new owner; queues draft OWNERSHIP_SYNC with isLinked=false.
    /// </summary>
    Task<UnlinkedDeviceResponseDto> UnlinkAsync(
        Guid deviceId,
        Guid actorUserId,
        CancellationToken ct = default);
}

public sealed class OwnershipConfirmationResultDto
{
    public Guid DeviceId { get; set; }
    public long ConfirmedOwnershipVersion { get; set; }
    public Guid? OwnershipId { get; set; }
    public string? OwnershipStatus { get; set; }
    public bool Activated { get; set; }
    public string ArmState { get; set; } = string.Empty;
    public bool CanControl { get; set; }
}
