using Application.Dtos.AdminDtos;

namespace Application.Interfaces.Services;

public interface IOwnerAdminService
{
    Task<OwnerAccountResponseDto> CreateAsync(CreateOwnerRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task<OwnerAccountResponseDto> UpdateAsync(Guid ownerId, UpdateOwnerRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task DisableAsync(Guid ownerId, Guid actorUserId, CancellationToken ct = default);
    Task ArchiveAsync(Guid ownerId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>Admin directory of OWNER accounts (FR-008). No passwords or tokens.</summary>
    Task<IReadOnlyList<AdminOwnerListItemDto>> ListOwnersAsync(CancellationToken ct = default);

    Task<AdminOwnerListItemDto> GetOwnerAsync(Guid ownerId, CancellationToken ct = default);
}
