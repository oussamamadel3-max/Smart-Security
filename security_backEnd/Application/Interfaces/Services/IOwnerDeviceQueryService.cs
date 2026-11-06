using Application.Dtos.OwnerDtos;

namespace Application.Interfaces.Services;

public interface IOwnerDeviceQueryService
{
    Task<IReadOnlyList<OwnerDeviceListItemDto>> ListMineAsync(Guid ownerUserId, CancellationToken ct = default);

    Task<OwnerDeviceDetailDto> GetMineAsync(Guid ownerUserId, Guid deviceId, CancellationToken ct = default);
}
