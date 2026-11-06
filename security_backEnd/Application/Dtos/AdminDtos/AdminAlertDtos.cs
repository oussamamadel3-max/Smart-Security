namespace Application.Dtos.AdminDtos;

public sealed class CreateAdminAlertRequestDto
{
    public Guid RecipientUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class AdminAlertItemDto
{
    public Guid AdminAlertId { get; set; }
    public Guid SenderUserId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Guid? NotificationId { get; set; }
    public string? NotificationStatus { get; set; }

    /// <summary>
    /// PENDING until a real FCM attempt completes; never invented as ACCEPTED without provider evidence.
    /// </summary>
    public string DeliveryAttemptStatus { get; set; } = string.Empty;

    public bool DeliveryAttempted { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
}

public sealed class AdminAlertQueryDto
{
    public Guid? RecipientUserId { get; set; }
    public Guid? SenderUserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class DeviceFaultAdminItemDto
{
    public Guid DeviceFaultId { get; set; }
    public Guid DeviceId { get; set; }
    public Guid FaultUid { get; set; }
    public Guid? OwnershipId { get; set; }
    public Guid? SensorId { get; set; }
    public string FaultCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? DeviceOccurredAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? Details { get; set; }
    public long OwnershipVersion { get; set; }
}

public sealed class DeviceFaultAdminQueryDto
{
    public Guid? DeviceId { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
