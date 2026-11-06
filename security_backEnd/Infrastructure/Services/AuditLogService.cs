using System.Text.Json;
using Application.Interfaces.Services;
using Domain.Entities.Operations;
using Infrastructure.Persistence;

namespace Infrastructure.Services;

public sealed class AuditLogService : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public Task WriteAsync(
        Guid? actorUserId,
        string action,
        string targetType,
        Guid? targetId,
        string result,
        object? safeDetails = null,
        Guid? deviceId = null,
        CancellationToken ct = default)
    {
        string? details = null;
        if (safeDetails is not null)
            details = JsonSerializer.Serialize(safeDetails, JsonOptions);

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DeviceId = deviceId,
            Result = result,
            OccurredAt = DateTimeOffset.UtcNow,
            Details = details
        });

        return Task.CompletedTask;
    }
}
