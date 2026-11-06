using Application.Dtos.AdminDtos;
using Application.Dtos.Common;

namespace Application.Interfaces.Services;

public interface IAdminAlertService
{
    /// <summary>
    /// Persists ADMIN_ALERT + notification request in one transaction (FR-026 / BR-016).
    /// Does not claim FCM success; delivery stays PENDING until T-09 attempts a real send.
    /// </summary>
    Task<AdminAlertItemDto> CreateAsync(
        CreateAdminAlertRequestDto request,
        Guid senderUserId,
        CancellationToken ct = default);

    Task<PagedResultDto<AdminAlertItemDto>> ListAsync(AdminAlertQueryDto query, CancellationToken ct = default);

    Task<AdminAlertItemDto> GetAsync(Guid adminAlertId, CancellationToken ct = default);

    /// <summary>
    /// Lists device-reported faults only (FR-025). Never invents faults from offline/heartbeat loss.
    /// </summary>
    Task<PagedResultDto<DeviceFaultAdminItemDto>> ListFaultsAsync(
        DeviceFaultAdminQueryDto query,
        CancellationToken ct = default);
}