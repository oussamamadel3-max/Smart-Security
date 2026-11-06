using Application.Dtos.OwnerDtos;

namespace Application.Interfaces.Services;

/// <summary>
/// Owner-scoped alarm/notification history with ownership isolation (FR-005 / FR-032).
/// </summary>
public interface IOwnerDeviceHistoryService
{
    Task<IReadOnlyList<OwnerAlarmEventListItemDto>> ListAlarmEventsAsync(
        Guid ownerUserId,
        Guid deviceId,
        CancellationToken ct = default);

    Task<OwnerAlarmEventListItemDto> GetAlarmEventAsync(
        Guid ownerUserId,
        Guid deviceId,
        Guid alarmEventId,
        CancellationToken ct = default);

    Task<IReadOnlyList<OwnerNotificationListItemDto>> ListNotificationsAsync(
        Guid ownerUserId,
        CancellationToken ct = default);
}
