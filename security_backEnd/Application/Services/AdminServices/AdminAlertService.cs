using Application.Dtos.AdminDtos;
using Application.Dtos.Common;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Events;
using Domain.Entities.Notifications;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Application.Services.AdminServices;

public sealed class AdminAlertService : IAdminAlertService
{
    private const int MaxTitleLength = 160;
    private const int MaxBodyLength = 4000;
    private const int MaxPageSize = 200;

    private readonly IAlarmEventRepository _alarms;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;
    private readonly UserManager<User> _users;

    public AdminAlertService(
        IAlarmEventRepository alarms,
        IUnitOfWork uow,
        IAuditLogService audit,
        UserManager<User> users)
    {
        _alarms = alarms;
        _uow = uow;
        _audit = audit;
        _users = users;
    }

    public async Task<AdminAlertItemDto> CreateAsync(
        CreateAdminAlertRequestDto request,
        Guid senderUserId,
        CancellationToken ct = default)
    {
        var title = NormalizeRequired(request.Title, MaxTitleLength, "عنوان التنبيه غير صالح.", "INVALID_ALERT_TITLE");
        var body = NormalizeRequired(request.Body, MaxBodyLength, "محتوى التنبيه غير صالح.", "INVALID_ALERT_BODY");

        var recipient = await _users.FindByIdAsync(request.RecipientUserId.ToString())
            ?? throw new NotFoundException("المستلم غير موجود.", "RECIPIENT_NOT_FOUND");
        if (recipient.DeletedAt is not null)
            throw new NotFoundException("المستلم غير موجود.", "RECIPIENT_NOT_FOUND");
        if (!await _users.IsInRoleAsync(recipient, DbStatusCodes.RoleCodes.Owner))
            throw new BadRequestException("التنبيه الإداري يُوجَّه لحساب مالك فقط.", "RECIPIENT_NOT_OWNER");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var alertId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var alert = new AdminAlert
            {
                Id = alertId,
                SenderUserId = senderUserId,
                RecipientUserId = recipient.Id,
                Title = title,
                Body = body,
                CreatedAt = now
            };
            _alarms.AddAdminAlert(alert);

            // Persist delivery request in the same transaction (BR-016). FCM worker is T-09 —
            // status stays QUEUED; API reports PENDING (never invent ACCEPTED).
            var notification = new Notification
            {
                Id = notificationId,
                RecipientUserId = recipient.Id,
                NotificationType = DbStatusCodes.NotificationType.AdminAlert,
                AdminAlertId = alertId,
                Status = DbStatusCodes.NotificationStatus.Queued,
                CreatedAt = now,
                LastAttemptAt = null,
                NextAttemptAt = now
            };
            _alarms.AddNotification(notification);

            await _audit.WriteAsync(
                senderUserId,
                AuditActions.AdminAlertCreated,
                AuditActions.TargetTypes.AdminAlert,
                alertId,
                DbStatusCodes.AuditResult.Success,
                new
                {
                    recipientUserId = recipient.Id,
                    notificationId,
                    notificationStatus = notification.Status,
                    deliveryAttemptStatus = AdminAlertDeliveryCodes.Pending,
                    deliveryAttempted = false
                },
                ct: ct);

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return MapAlert(alert, notification);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<PagedResultDto<AdminAlertItemDto>> ListAsync(
        AdminAlertQueryDto query,
        CancellationToken ct = default)
    {
        var (page, pageSize, skip) = NormalizePage(query.Page, query.PageSize);
        var (items, total) = await _alarms.QueryAdminAlertsAsync(
            query.RecipientUserId,
            query.SenderUserId,
            skip,
            pageSize,
            ct);

        return new PagedResultDto<AdminAlertItemDto>
        {
            Items = items.Select(x => MapAlert(x.Alert, x.Notification)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AdminAlertItemDto> GetAsync(Guid adminAlertId, CancellationToken ct = default)
    {
        var alert = await _alarms.GetAdminAlertByIdAsync(adminAlertId, ct)
            ?? throw new NotFoundException("التنبيه الإداري غير موجود.", "ADMIN_ALERT_NOT_FOUND");
        var notification = await _alarms.FindAdminAlertNotificationAsync(alert.RecipientUserId, alert.Id, ct);
        return MapAlert(alert, notification);
    }

    public async Task<PagedResultDto<DeviceFaultAdminItemDto>> ListFaultsAsync(
        DeviceFaultAdminQueryDto query,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(query.Status)
            && query.Status is not (DbStatusCodes.Fault.Active or DbStatusCodes.Fault.Resolved))
        {
            throw new BadRequestException("حالة العطل غير صالحة.", "INVALID_FAULT_STATUS");
        }

        var (page, pageSize, skip) = NormalizePage(query.Page, query.PageSize);
        var (items, total) = await _alarms.QueryDeviceFaultsAsync(
            query.DeviceId,
            string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim(),
            skip,
            pageSize,
            ct);

        return new PagedResultDto<DeviceFaultAdminItemDto>
        {
            Items = items.Select(MapFault).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private static AdminAlertItemDto MapAlert(AdminAlert alert, Notification? notification)
    {
        var status = notification?.Status;
        var lastAttemptAt = notification?.LastAttemptAt;
        // FAILED/ACCEPTED only when last_attempt_at proves a real send attempt (T-09).
        var delivery = AdminAlertDeliveryCodes.FromNotificationStatus(status, lastAttemptAt);
        var attempted = lastAttemptAt is not null;

        return new AdminAlertItemDto
        {
            AdminAlertId = alert.Id,
            SenderUserId = alert.SenderUserId,
            RecipientUserId = alert.RecipientUserId,
            Title = alert.Title,
            Body = alert.Body,
            CreatedAt = alert.CreatedAt,
            NotificationId = notification?.Id,
            NotificationStatus = status,
            DeliveryAttemptStatus = delivery,
            DeliveryAttempted = attempted,
            LastAttemptAt = lastAttemptAt
        };
    }

    private static DeviceFaultAdminItemDto MapFault(DeviceFault fault) => new()
    {
        DeviceFaultId = fault.Id,
        DeviceId = fault.DeviceId,
        FaultUid = fault.FaultUid,
        OwnershipId = fault.OwnershipId,
        SensorId = fault.SensorId,
        FaultCode = fault.FaultCode,
        Status = fault.Status,
        DeviceOccurredAt = fault.DeviceOccurredAt,
        ReceivedAt = fault.ReceivedAt,
        ResolvedAt = fault.ResolvedAt,
        Details = fault.Details,
        OwnershipVersion = fault.OwnershipVersion
    };

    private static (int Page, int PageSize, int Skip) NormalizePage(int page, int pageSize)
    {
        var p = page < 1 ? 1 : page;
        var size = pageSize < 1 ? 50 : Math.Min(pageSize, MaxPageSize);
        return (p, size, (p - 1) * size);
    }

    private static string NormalizeRequired(string? value, int maxLength, string message, string code)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 || trimmed.Length > maxLength)
            throw new BadRequestException(message, code);
        return trimmed;
    }
}
