namespace Application.Dtos.OwnerDtos;

public sealed class OwnerAlarmEventListItemDto
{
    public Guid AlarmEventId { get; set; }
    public Guid DeviceId { get; set; }
    public Guid EventUid { get; set; }
    public Guid OwnershipId { get; set; }
    public long OwnershipVersion { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? OccurredDeviceAt { get; set; }
    public bool ClockTrusted { get; set; }
    public bool WasBuffered { get; set; }
    public DateTimeOffset? MutedAt { get; set; }
    public string SensorKeySnapshot { get; set; } = string.Empty;
}

public sealed class OwnerNotificationListItemDto
{
    public Guid NotificationId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public Guid? AlarmEventId { get; set; }
    public Guid? DeviceFaultId { get; set; }
    public Guid? AdminAlertId { get; set; }
}
