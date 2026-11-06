using Application.Dtos.PushTokenDtos;

namespace Application.Interfaces.Services;

public interface IPushTokenService
{
    Task<PushTokenResponseDto> RegisterOrUpdateAsync(
        Guid userId,
        RegisterPushTokenRequestDto request,
        CancellationToken ct = default);

    Task<IReadOnlyList<PushTokenListItemDto>> ListMineAsync(
        Guid userId,
        CancellationToken ct = default);

    Task RevokeAsync(
        Guid userId,
        Guid pushTokenId,
        CancellationToken ct = default);

    Task RevokeAllActiveAsync(
        Guid userId,
        CancellationToken ct = default);
}
