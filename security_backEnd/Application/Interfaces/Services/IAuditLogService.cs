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
}
