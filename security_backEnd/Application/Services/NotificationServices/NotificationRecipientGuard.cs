using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;

namespace Application.Services.NotificationServices;

/// <summary>
/// Pre-send eligibility check for ALARM/FAULT notifications (T-06 prep; FCM worker is T-09).
/// Does not implement ownership-transfer bulk cancel (T-07).
/// </summary>
public sealed class NotificationRecipientGuard : INotificationRecipientGuard
{
    private readonly IAlarmEventRepository _alarms;
    private readonly IUnitOfWork _uow;

    public NotificationRecipientGuard(IAlarmEventRepository alarms, IUnitOfWork uow)
    {
        _alarms = alarms;
        _uow = uow;
    }

    public async Task<bool> EnsureRecipientStillEligibleAsync(
        Guid notificationId,
        CancellationToken ct = default)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var notification = await _alarms.GetNotificationForUpdateAsync(notificationId, ct);
            if (notification is null)
            {
                await tx.RollbackAsync(ct);
                return false;
            }

            if (notification.Status == DbStatusCodes.NotificationStatus.Cancelled)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            if (notification.Status is DbStatusCodes.NotificationStatus.Accepted
                or DbStatusCodes.NotificationStatus.Failed)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            var eligible = notification.NotificationType switch
            {
                DbStatusCodes.NotificationType.Alarm when notification.AlarmEventId is Guid alarmId
                    => await IsAlarmRecipientEligibleAsync(notification.RecipientUserId, alarmId, ct),
                DbStatusCodes.NotificationType.Fault when notification.DeviceFaultId is Guid faultId
                    => await IsFaultRecipientEligibleAsync(notification.RecipientUserId, faultId, ct),
                DbStatusCodes.NotificationType.AdminAlert
                    => true,
                _ => false
            };

            if (!eligible)
            {
                var now = DateTimeOffset.UtcNow;
                notification.Status = DbStatusCodes.NotificationStatus.Cancelled;
                notification.CancelledAt = now;
                notification.CancellationReason = EventPersistedDraft.CancellationReasons.OwnershipNotActive;
                await _uow.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return eligible;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<bool> IsAlarmRecipientEligibleAsync(
        Guid recipientUserId,
        Guid alarmEventId,
        CancellationToken ct)
    {
        var alarm = await _alarms.GetByIdAsync(alarmEventId, ct);
        if (alarm is null || alarm.OwnershipId is null)
            return false;

        var ownership = await _alarms.FindOwnershipByVersionForUpdateAsync(
            alarm.DeviceId,
            alarm.OwnershipVersion,
            ct);

        return ownership is not null
               && ownership.Id == alarm.OwnershipId
               && ownership.OwnerUserId == recipientUserId
               && ownership.Status == DbStatusCodes.Ownership.Active
               && ownership.EndedAt is null;
    }

    private async Task<bool> IsFaultRecipientEligibleAsync(
        Guid recipientUserId,
        Guid deviceFaultId,
        CancellationToken ct)
    {
        var fault = await _alarms.GetFaultByIdAsync(deviceFaultId, ct);
        if (fault is null || fault.OwnershipId is null)
            return false;

        var ownership = await _alarms.FindOwnershipByVersionForUpdateAsync(
            fault.DeviceId,
            fault.OwnershipVersion,
            ct);

        return ownership is not null
               && ownership.Id == fault.OwnershipId
               && ownership.OwnerUserId == recipientUserId
               && ownership.Status == DbStatusCodes.Ownership.Active
               && ownership.EndedAt is null;
    }
}
