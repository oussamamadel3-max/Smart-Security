using Application.Dtos.AdminDtos;

namespace Application.Interfaces.Services;

public interface IOwnerAdminService
{
    Task<OwnerAccountResponseDto> CreateAsync(CreateOwnerRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task<OwnerAccountResponseDto> UpdateAsync(Guid ownerId, UpdateOwnerRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task DisableAsync(Guid ownerId, Guid actorUserId, CancellationToken ct = default);
    Task ArchiveAsync(Guid ownerId, Guid actorUserId, CancellationToken ct = default);
}
