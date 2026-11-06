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
}
