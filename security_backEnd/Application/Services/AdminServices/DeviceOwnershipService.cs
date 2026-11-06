using System.Text.Json;
using Application.Dtos.AdminDtos;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Devices;
using Domain.Entities.Operations;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AdminServices;

public sealed class DeviceOwnershipService : IDeviceOwnershipService
{
    private readonly IDeviceRepository _devices;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;
    private readonly UserManager<User> _users;

    public DeviceOwnershipService(
        IDeviceRepository devices,
        IUnitOfWork uow,
        IAuditLogService audit,
        UserManager<User> users)
    {
        _devices = devices;
        _uow = uow;
        _audit = audit;
        _users = users;
    }

    public async Task<LinkedDeviceResponseDto> LinkInitialAsync(
        Guid deviceId,
        LinkDeviceRequestDto request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        if (request is null || request.OwnerUserId == Guid.Empty)
            throw new BadRequestException("معرف المالك مطلوب.", "OWNER_REQUIRED");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var device = await _devices.GetByIdAsync(deviceId, ct)
                ?? throw new NotFoundException("الجهاز غير موجود.", "DEVICE_NOT_FOUND");

            if (device.LifecycleStatus == DbStatusCodes.DeviceLifecycle.Retired)
                throw new ConflictException("لا يمكن ربط جهاز مؤرشف.", "DEVICE_RETIRED");

            if (await _devices.HasOpenOwnershipAsync(deviceId, ct))
                throw new ConflictException("الجهاز مرتبط بمالك بالفعل.", "DEVICE_ALREADY_LINKED");

            var owner = await RequireLinkableOwnerAsync(request.OwnerUserId);
            var state = await _devices.GetStateAsync(deviceId, ct)
                ?? throw new ConflictException("حالة الجهاز غير مهيأة.", "DEVICE_STATE_MISSING");

            var armBeforeLink = state.ArmState;
            var now = DateTimeOffset.UtcNow;
            var ownershipVersion = device.CurrentOwnershipVersion + 1;
            var ownershipId = Guid.NewGuid();
            var messageId = Guid.NewGuid();

            var ownership = new DeviceOwnership
            {
                Id = ownershipId,
                DeviceId = deviceId,
                OwnerUserId = owner.Id,
                Version = ownershipVersion,
                Status = DbStatusCodes.Ownership.Pending,
                StartedAt = now,
                ActivatedAt = null,
                EndedAt = null,
                CreatedByUserId = actorUserId,
                EndedByUserId = null
            };

            device.CurrentOwnershipVersion = ownershipVersion;
            device.UpdatedAt = now;
            // Linking must not change protection / lifecycle readiness claim.
            // Lifecycle stays REGISTERED until device acknowledges the ownership version.

            var payload = BuildOwnershipSyncPayload(
                deviceId,
                messageId,
                ownershipVersion,
                isLinked: true);

            var outbox = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateType = OwnershipSyncDraft.OutboxAggregateType,
                AggregateId = ownershipId,
                MessageType = OwnershipSyncDraft.OutboxPublishMessageType,
                Payload = payload,
                CreatedAt = now,
                AvailableAt = now,
                ProcessedAt = null,
                AttemptCount = 0,
                LastErrorCode = null
            };

            _devices.AddOwnership(ownership);
            _devices.AddOutboxMessage(outbox);

            await _audit.WriteAsync(
                actorUserId,
                AuditActions.DeviceLinked,
                AuditActions.TargetTypes.Device,
                deviceId,
                DbStatusCodes.AuditResult.Success,
                new
                {
                    ownershipId,
                    ownerUserId = owner.Id,
                    ownershipVersion,
                    ownershipStatus = ownership.Status,
                    armStateUnchanged = armBeforeLink,
                    ownershipSyncMessageType = OwnershipSyncDraft.MqttMessageType,
                    outboxMessageType = OwnershipSyncDraft.OutboxPublishMessageType
                },
                deviceId,
                ct);

            try
            {
                await _uow.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsOpenOwnershipViolation(ex))
            {
                throw new ConflictException("الجهاز مرتبط بمالك بالفعل.", "DEVICE_ALREADY_LINKED");
            }

            await tx.CommitAsync(ct);

            return new LinkedDeviceResponseDto
            {
                DeviceId = device.Id,
                DisplayName = device.DisplayName,
                DeviceLifecycleStatus = device.LifecycleStatus,
                OwnershipId = ownership.Id,
                OwnerUserId = ownership.OwnerUserId,
                OwnershipStatus = ownership.Status,
                OwnershipVersion = ownership.Version,
                ConfirmedOwnershipVersion = state.ConfirmedOwnershipVersion,
                ArmState = state.ArmState,
                CanControl = false,
                OwnershipSyncQueued = true,
                OwnershipSyncMessageType = OwnershipSyncDraft.MqttMessageType,
                LinkedAt = now
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<OwnershipConfirmationResultDto> ConfirmOwnershipVersionAsync(
        Guid deviceId,
        long confirmedOwnershipVersion,
        CancellationToken ct = default)
    {
        if (confirmedOwnershipVersion < 0)
            throw new BadRequestException("إصدار الملكية المؤكد غير صالح.", "INVALID_OWNERSHIP_VERSION");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var device = await _devices.GetByIdAsync(deviceId, ct)
                ?? throw new NotFoundException("الجهاز غير موجود.", "DEVICE_NOT_FOUND");

            if (device.LifecycleStatus == DbStatusCodes.DeviceLifecycle.Retired)
                throw new ConflictException("لا يمكن إقرار ملكية لجهاز مؤرشف.", "DEVICE_RETIRED");

            var state = await _devices.GetStateAsync(deviceId, ct)
                ?? throw new ConflictException("حالة الجهاز غير مهيأة.", "DEVICE_STATE_MISSING");

            var armBefore = state.ArmState;
            var activated = false;
            DeviceOwnership? open = null;

            // Monotonic confirmation only — never roll confirmed version backwards.
            if (confirmedOwnershipVersion > state.ConfirmedOwnershipVersion)
                state.ConfirmedOwnershipVersion = confirmedOwnershipVersion;

            open = await _devices.GetOpenOwnershipAsync(deviceId, ct);
            if (open is not null
                && open.Status == DbStatusCodes.Ownership.Pending
                && open.Version == state.ConfirmedOwnershipVersion)
            {
                var now = DateTimeOffset.UtcNow;
                open.Status = DbStatusCodes.Ownership.Active;
                open.ActivatedAt = now;
                // Effective ownership start for initial link is the acknowledgment instant.
                open.StartedAt = now;

                if (device.LifecycleStatus == DbStatusCodes.DeviceLifecycle.Registered)
                    device.LifecycleStatus = DbStatusCodes.DeviceLifecycle.Active;

                device.UpdatedAt = now;
                activated = true;

                await _audit.WriteAsync(
                    actorUserId: null,
                    AuditActions.OwnershipActivated,
                    AuditActions.TargetTypes.Ownership,
                    open.Id,
                    DbStatusCodes.AuditResult.Success,
                    new
                    {
                        deviceId,
                        ownershipId = open.Id,
                        ownershipVersion = open.Version,
                        confirmedOwnershipVersion = state.ConfirmedOwnershipVersion,
                        armStateUnchanged = armBefore
                    },
                    deviceId,
                    ct);
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var canControl = open is not null
                             && open.Status == DbStatusCodes.Ownership.Active
                             && state.ConfirmedOwnershipVersion >= open.Version;

            return new OwnershipConfirmationResultDto
            {
                DeviceId = deviceId,
                ConfirmedOwnershipVersion = state.ConfirmedOwnershipVersion,
                OwnershipId = open?.Id,
                OwnershipStatus = open?.Status,
                Activated = activated,
                ArmState = state.ArmState,
                CanControl = canControl
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<User> RequireLinkableOwnerAsync(Guid ownerUserId)
    {
        var user = await _users.FindByIdAsync(ownerUserId.ToString())
            ?? throw new NotFoundException("حساب المالك غير موجود.", "OWNER_NOT_FOUND");

        if (!await _users.IsInRoleAsync(user, DbStatusCodes.RoleCodes.Owner))
            throw new NotFoundException("حساب المالك غير موجود.", "OWNER_NOT_FOUND");

        if (!user.IsActive || user.DeletedAt is not null)
            throw new ConflictException("حساب المالك غير نشط.", "OWNER_INACTIVE");

        return user;
    }

    private static string BuildOwnershipSyncPayload(
        Guid deviceId,
        Guid messageId,
        long ownershipVersion,
        bool isLinked)
    {
        // Draft shape from development-plan §4.4 — no owner PII.
        var payload = new Dictionary<string, object?>
        {
            ["schemaVersion"] = OwnershipSyncDraft.SchemaVersion,
            ["deviceId"] = deviceId,
            ["messageId"] = messageId,
            ["messageType"] = OwnershipSyncDraft.MqttMessageType,
            ["ownershipVersion"] = ownershipVersion,
            ["isLinked"] = isLinked
        };

        return JsonSerializer.Serialize(payload);
    }

    private static bool IsOpenOwnershipViolation(DbUpdateException ex)
    {
        var text = ex.InnerException?.Message ?? ex.Message;
        return text.Contains("ux_device_open_ownership", StringComparison.OrdinalIgnoreCase);
    }
}
