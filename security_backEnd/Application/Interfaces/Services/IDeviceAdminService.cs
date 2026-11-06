using Application.Dtos.AdminDtos;

namespace Application.Interfaces.Services;

public interface IDeviceAdminService
{
    Task<RegisteredDeviceResponseDto> RegisterAsync(
        RegisterDeviceRequestDto request,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Archives a device (RETIRED), ends open ownership, revokes credentials and broker access (FR-007).
    /// </summary>
    Task<ArchivedDeviceResponseDto> ArchiveAsync(
        Guid deviceId,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Admin overview of devices with owners, computed connectivity, and last known state (FR-008).
    /// Does not grant ARM/DISARM/MUTE/TEST.
    /// </summary>
    Task<IReadOnlyList<AdminDeviceOverviewDto>> ListOverviewAsync(CancellationToken ct = default);

    Task<AdminDeviceOverviewDto> GetOverviewAsync(Guid deviceId, CancellationToken ct = default);
}
