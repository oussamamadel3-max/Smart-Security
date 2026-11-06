namespace Application.Dtos.OwnerDtos;

/// <summary>
/// Owner-facing device row. PENDING rows are intentionally limited (FR-035): name + wait state only.
/// </summary>
public sealed class OwnerDeviceListItemDto
{
    public Guid DeviceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string OwnershipStatus { get; set; } = string.Empty;
    public long OwnershipVersion { get; set; }
    public long ConfirmedOwnershipVersion { get; set; }
    public bool AwaitingOwnershipAck { get; set; }
    public bool CanControl { get; set; }
    public string LinkStatusMessage { get; set; } = string.Empty;
}
