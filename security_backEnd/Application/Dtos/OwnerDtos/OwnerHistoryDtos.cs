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

/// <summary>
/// Notification detail after ownership re-check (FCM-05). Provider ACCEPTED is not user-read.
/// </summary>
public sealed class OwnerNotificationDetailDto
{
    public Guid NotificationId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public Guid? DeviceId { get; set; }
    public OwnerAlarmEventListItemDto? AlarmEvent { get; set; }
    public OwnerFaultDetailDto? DeviceFault { get; set; }
    public OwnerAdminAlertDetailDto? AdminAlert { get; set; }
}

public sealed class OwnerFaultDetailDto
{
    public Guid DeviceFaultId { get; set; }
    public Guid DeviceId { get; set; }
    public string FaultCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class OwnerAdminAlertDetailDto
{
    public Guid AdminAlertId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
