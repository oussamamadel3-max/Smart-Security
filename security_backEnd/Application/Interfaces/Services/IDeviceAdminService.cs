using Application.Dtos.AdminDtos;

namespace Application.Interfaces.Services;

public interface IDeviceAdminService
{
    Task<RegisteredDeviceResponseDto> RegisterAsync(
        RegisterDeviceRequestDto request,
        Guid actorUserId,
        CancellationToken ct = default);
}
