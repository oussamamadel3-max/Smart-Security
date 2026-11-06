using Application.Dtos.OwnerDtos;

namespace Application.Interfaces.Services;

public interface IOwnerDeviceCommandService
{
    Task<OwnerDeviceCommandResponseDto> CreateAsync(
        Guid ownerUserId,
        Guid deviceId,
        CreateOwnerDeviceCommandRequestDto request,
        CancellationToken ct = default);

    Task<OwnerDeviceCommandResponseDto> GetMineAsync(
        Guid ownerUserId,
        Guid deviceId,
        Guid commandId,
        CancellationToken ct = default);
}
