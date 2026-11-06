using Application.Dtos.AdminDtos;
using Application.Dtos.Common;

namespace Application.Interfaces.Services;

public interface IAuditLogService
{
    Task WriteAsync(
        Guid? actorUserId,
        string action,
        string targetType,
        Guid? targetId,
        string result,
        object? safeDetails = null,
        Guid? deviceId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Read-only query over <c>audit_logs</c> (FR-033). No update/delete surface.
    /// </summary>
    Task<PagedResultDto<AuditLogItemDto>> QueryAsync(AuditLogQueryDto query, CancellationToken ct = default);
}
