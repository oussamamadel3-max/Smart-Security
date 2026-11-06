using System.Text.Json;
using Application.Dtos.AdminDtos;
using Application.Dtos.Common;
using Application.Interfaces.Services;
using Domain.Entities.Operations;
using Domain.Exceptions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class AuditLogService : IAuditLogService
{
    private const int MaxPageSize = 200;

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

    public async Task<PagedResultDto<AuditLogItemDto>> QueryAsync(
        AuditLogQueryDto query,
        CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 50 : Math.Min(query.PageSize, MaxPageSize);
        var sortBy = (query.SortBy ?? "occurredAt").Trim().ToLowerInvariant();
        var sortDir = (query.SortDir ?? "desc").Trim().ToLowerInvariant();
        if (sortDir is not ("asc" or "desc"))
            throw new BadRequestException("اتجاه الفرز غير صالح.", "INVALID_SORT_DIR");

        var allowedSort = new HashSet<string>(StringComparer.Ordinal)
        {
            "occurredat", "action", "result", "actoruserid", "deviceid"
        };
        if (!allowedSort.Contains(sortBy))
            throw new BadRequestException("حقل الفرز غير مسموح.", "INVALID_SORT_BY");

        IQueryable<AuditLog> q = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(a => a.Action == query.Action.Trim());
        if (!string.IsNullOrWhiteSpace(query.TargetType))
            q = q.Where(a => a.TargetType == query.TargetType.Trim());
        if (query.ActorUserId is Guid actor)
            q = q.Where(a => a.ActorUserId == actor);
        if (query.DeviceId is Guid deviceId)
            q = q.Where(a => a.DeviceId == deviceId);
        if (query.TargetId is Guid targetId)
            q = q.Where(a => a.TargetId == targetId);
        if (!string.IsNullOrWhiteSpace(query.Result))
            q = q.Where(a => a.Result == query.Result.Trim());
        if (query.From is DateTimeOffset from)
            q = q.Where(a => a.OccurredAt >= from);
        if (query.To is DateTimeOffset to)
            q = q.Where(a => a.OccurredAt <= to);
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            q = q.Where(a =>
                EF.Functions.ILike(a.Action, $"%{term}%")
                || EF.Functions.ILike(a.TargetType, $"%{term}%")
                || (a.Details != null && EF.Functions.ILike(a.Details, $"%{term}%")));
        }

        var total = await q.LongCountAsync(ct);

        q = (sortBy, sortDir) switch
        {
            ("action", "asc") => q.OrderBy(a => a.Action).ThenByDescending(a => a.Id),
            ("action", _) => q.OrderByDescending(a => a.Action).ThenByDescending(a => a.Id),
            ("result", "asc") => q.OrderBy(a => a.Result).ThenByDescending(a => a.Id),
            ("result", _) => q.OrderByDescending(a => a.Result).ThenByDescending(a => a.Id),
            ("actoruserid", "asc") => q.OrderBy(a => a.ActorUserId).ThenByDescending(a => a.Id),
            ("actoruserid", _) => q.OrderByDescending(a => a.ActorUserId).ThenByDescending(a => a.Id),
            ("deviceid", "asc") => q.OrderBy(a => a.DeviceId).ThenByDescending(a => a.Id),
            ("deviceid", _) => q.OrderByDescending(a => a.DeviceId).ThenByDescending(a => a.Id),
            ("occurredat", "asc") => q.OrderBy(a => a.OccurredAt).ThenBy(a => a.Id),
            _ => q.OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogItemDto
            {
                Id = a.Id,
                ActorUserId = a.ActorUserId,
                Action = a.Action,
                TargetType = a.TargetType,
                TargetId = a.TargetId,
                DeviceId = a.DeviceId,
                Result = a.Result,
                OccurredAt = a.OccurredAt,
                Details = a.Details
            })
            .ToListAsync(ct);

        return new PagedResultDto<AuditLogItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
}
