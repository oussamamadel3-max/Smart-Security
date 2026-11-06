namespace Application.Dtos.AdminDtos;

public sealed class AuditLogQueryDto
{
    public string? Action { get; set; }
    public string? TargetType { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? DeviceId { get; set; }
    public Guid? TargetId { get; set; }
    public string? Result { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Case-insensitive contains search over action / targetType / details (safe JSON text only).
    /// </summary>
    public string? Q { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Whitelist: occurredAt | action | result | actorUserId | deviceId.
    /// </summary>
    public string SortBy { get; set; } = "occurredAt";

    /// <summary>
    /// asc | desc — default desc for occurredAt.
    /// </summary>
    public string SortDir { get; set; } = "desc";
}

public sealed class AuditLogItemDto
{
    public long Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public Guid? DeviceId { get; set; }
    public string Result { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string? Details { get; set; }
}
