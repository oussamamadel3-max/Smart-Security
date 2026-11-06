using System.Text.Json;
using Application.Common.Devices;
using Application.Dtos.OwnerDtos;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Commands;
using Domain.Entities.Operations;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.OwnerServices;

/// <summary>
/// Creates owner device commands with ownership/connectivity/conflict gates.
/// HTTP 202 / PENDING never means the device executed the command.
/// </summary>
public sealed class OwnerDeviceCommandService : IOwnerDeviceCommandService
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.Ordinal)
    {
        DbStatusCodes.CommandType.Arm,
        DbStatusCodes.CommandType.Disarm,
        DbStatusCodes.CommandType.MuteAlarm,
        DbStatusCodes.CommandType.TestSiren,
        DbStatusCodes.CommandType.StopTest
    };

    private readonly IDeviceRepository _devices;
    private readonly IDeviceCommandRepository _commands;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;

    public OwnerDeviceCommandService(
        IDeviceRepository devices,
        IDeviceCommandRepository commands,
        IUnitOfWork uow,
        IAuditLogService audit)
    {
        _devices = devices;
        _commands = commands;
        _uow = uow;
        _audit = audit;
    }

    public async Task<OwnerDeviceCommandResponseDto> CreateAsync(
        Guid ownerUserId,
        Guid deviceId,
        CreateOwnerDeviceCommandRequestDto request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IdempotencyKey == Guid.Empty)
            throw new BadRequestException("مفتاح Idempotency مطلوب.", "IDEMPOTENCY_KEY_REQUIRED");

        var commandType = (request.Type ?? string.Empty).Trim().ToUpperInvariant();
        if (!OwnerDeviceCommandAcceptanceRules.IsSupportedCommandType(commandType)
            || !SupportedTypes.Contains(commandType))
            throw new BadRequestException("نوع الأمر غير مدعوم.", ExecuteCommandDraft.RejectCodes.UnsupportedCommandType);

        // Fast idempotent replay outside the write lock when possible.
        var existing = await _commands.FindByIdempotencyAsync(ownerUserId, request.IdempotencyKey, ct);
        if (existing is not null)
            return MapReplayOrConflict(existing, ownerUserId, deviceId, commandType, request.TargetAlarmEventUid);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            // Re-check idempotency under the transaction after locking device state.
            existing = await _commands.FindByIdempotencyAsync(ownerUserId, request.IdempotencyKey, ct);
            if (existing is not null)
            {
                await tx.CommitAsync(ct);
                return MapReplayOrConflict(existing, ownerUserId, deviceId, commandType, request.TargetAlarmEventUid);
            }

            // Ownership gate first (404), then row-lock state so concurrent creators serialize.
            var ownership = await _devices.GetOpenOwnershipAsync(deviceId, ct);
            if (ownership is null || ownership.OwnerUserId != ownerUserId)
                throw new NotFoundException("الجهاز غير موجود ضمن ملكيتك.", "DEVICE_NOT_FOUND");

            var device = await _devices.GetByIdAsync(deviceId, ct)
                ?? throw new NotFoundException("الجهاز غير موجود ضمن ملكيتك.", "DEVICE_NOT_FOUND");

            var state = await _devices.GetStateForUpdateAsync(deviceId, ct)
                ?? throw new ConflictException("حالة الجهاز غير مهيأة.", "DEVICE_STATE_MISSING");

            // Re-read ownership after the lock — transfer/archive must not race a stale ACTIVE check.
            ownership = await _devices.GetOpenOwnershipAsync(deviceId, ct);
            if (ownership is null || ownership.OwnerUserId != ownerUserId)
                throw new NotFoundException("الجهاز غير موجود ضمن ملكيتك.", "DEVICE_NOT_FOUND");

            var settings = await _devices.GetSettingsAsync(deviceId, ct)
                ?? throw new ConflictException("إعدادات الجهاز غير مهيأة.", "DEVICE_SETTINGS_MISSING");

            var now = DateTimeOffset.UtcNow;

            if (device.LifecycleStatus == DbStatusCodes.DeviceLifecycle.Retired)
                throw new ConflictException("الجهاز مؤرشف.", "DEVICE_RETIRED");

            // ACTIVE ownership may accept a command request immediately (FR-006 / BR-015).
            // Device-confirmed ownership sync is enforced at Outbox publish time, not here.
            if (!OwnerDeviceCommandAcceptanceRules.IsOwnershipControllable(ownership.Status))
            {
                throw new ConflictException(
                    "التحكم غير متاح قبل تفعيل الملكية.",
                    ExecuteCommandDraft.RejectCodes.OwnershipNotControllable);
            }

            if (!DeviceConnectivity.IsOnline(state.LastSeenAt, settings.HeartbeatTimeoutSeconds, now))
            {
                throw new ConflictException(
                    "الجهاز غير متصل؛ رُفض التحكم اللحظي.",
                    ExecuteCommandDraft.RejectCodes.DeviceOffline);
            }

            if (await _commands.HasBlockingUnresolvedCommandAsync(deviceId, ct))
            {
                throw new ConflictException(
                    "يوجد أمر معلّق أو منتهي المهلة بانتظار المصالحة.",
                    ExecuteCommandDraft.RejectCodes.AwaitingReconciliation);
            }

            ValidateTypeSpecificRules(commandType, request.TargetAlarmEventUid, state);

            if (settings.CommandTtlSeconds <= 0)
                throw new ConflictException("صلاحية الأمر غير معتمدة للجهاز.", "COMMAND_TTL_NOT_CONFIGURED");

            var nextSequence = await _commands.GetMaxCommandSequenceAsync(deviceId, ct) + 1;
            var commandId = Guid.NewGuid();
            var messageId = Guid.NewGuid();
            var expiresAt = now.AddSeconds(settings.CommandTtlSeconds);

            var command = new DeviceCommand
            {
                Id = commandId,
                DeviceId = deviceId,
                OwnershipId = ownership.Id,
                OwnershipVersion = ownership.Version,
                RequestedByUserId = ownerUserId,
                CommandSequence = nextSequence,
                CommandType = commandType,
                AlarmEventId = null,
                Status = DbStatusCodes.CommandStatus.Pending,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                SentAt = null,
                AcknowledgedAt = null,
                RejectCode = null,
                IdempotencyKey = request.IdempotencyKey,
                ResultStateSequence = null,
                TargetAlarmEventUid = commandType == DbStatusCodes.CommandType.MuteAlarm
                    ? request.TargetAlarmEventUid
                    : null,
                ObservedStateSequence = state.StateSequence,
                ReconciledAt = null
            };

            var history = new CommandStatusHistory
            {
                CommandId = commandId,
                FromStatus = null,
                ToStatus = DbStatusCodes.CommandStatus.Pending,
                ReasonCode = ExecuteCommandDraft.HistoryReasons.Accepted,
                ChangedAt = now,
                IsLateAck = false
            };

            var payload = BuildExecuteCommandPayload(command, messageId);
            var outbox = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateType = ExecuteCommandDraft.OutboxAggregateType,
                AggregateId = commandId,
                MessageType = ExecuteCommandDraft.OutboxPublishMessageType,
                Payload = payload,
                CreatedAt = now,
                AvailableAt = now,
                ProcessedAt = null,
                AttemptCount = 0,
                LastErrorCode = null
            };

            _commands.AddCommand(command);
            _commands.AddStatusHistory(history);
            _commands.AddOutboxMessage(outbox);

            await _audit.WriteAsync(
                ownerUserId,
                AuditActions.DeviceCommandAccepted,
                AuditActions.TargetTypes.Command,
                commandId,
                DbStatusCodes.AuditResult.Success,
                new
                {
                    deviceId,
                    commandId,
                    commandType,
                    commandSequence = nextSequence,
                    ownershipVersion = ownership.Version,
                    observedStateSequence = state.StateSequence,
                    confirmedArmState = state.ArmState,
                    confirmedSirenState = state.SirenState,
                    outboxMessageType = ExecuteCommandDraft.OutboxPublishMessageType,
                    mqttMessageType = ExecuteCommandDraft.MqttMessageType,
                    actualStateChanged = false
                },
                deviceId,
                ct);

            try
            {
                await _uow.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsPendingConflict(ex))
            {
                throw new ConflictException(
                    "يوجد أمر معلّق للجهاز.",
                    ExecuteCommandDraft.RejectCodes.CommandConflict);
            }
            catch (DbUpdateException ex) when (IsIdempotencyConflict(ex))
            {
                // Concurrent idempotent insert — return the winner.
                var raced = await _commands.FindByIdempotencyAsync(ownerUserId, request.IdempotencyKey, ct)
                    ?? throw new ConflictException(
                        "تعارض مفتاح Idempotency.",
                        ExecuteCommandDraft.RejectCodes.IdempotencyConflict);
                await tx.RollbackAsync(ct);
                return MapReplayOrConflict(raced, ownerUserId, deviceId, commandType, request.TargetAlarmEventUid);
            }

            await tx.CommitAsync(ct);

            return Map(command, state.ArmState, state.SirenState, idempotentReplay: false);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<OwnerDeviceCommandResponseDto> GetMineAsync(
        Guid ownerUserId,
        Guid deviceId,
        Guid commandId,
        CancellationToken ct = default)
    {
        var row = await _devices.GetOpenOwnershipDeviceForOwnerAsync(ownerUserId, deviceId, ct)
            ?? throw new NotFoundException("الجهاز غير موجود ضمن ملكيتك.", "DEVICE_NOT_FOUND");

        var command = await _commands.GetByIdAsync(commandId, ct);
        // Isolate by current ownership period — prior-owner history never leaks (FR-032).
        if (command is null
            || command.DeviceId != deviceId
            || command.OwnershipId != row.Ownership.Id)
        {
            throw new NotFoundException("الأمر غير موجود.", "COMMAND_NOT_FOUND");
        }

        var state = await _devices.GetStateAsync(deviceId, ct);
        return Map(command, state?.ArmState, state?.SirenState, idempotentReplay: false);
    }

    public async Task<IReadOnlyList<OwnerDeviceCommandResponseDto>> ListMineAsync(
        Guid ownerUserId,
        Guid deviceId,
        CancellationToken ct = default)
    {
        var row = await _devices.GetOpenOwnershipDeviceForOwnerAsync(ownerUserId, deviceId, ct)
            ?? throw new NotFoundException("الجهاز غير موجود ضمن ملكيتك.", "DEVICE_NOT_FOUND");

        var commands = await _commands.ListForOwnershipAsync(deviceId, row.Ownership.Id, ct);
        var state = await _devices.GetStateAsync(deviceId, ct);
        return commands
            .Select(c => Map(c, state?.ArmState, state?.SirenState, idempotentReplay: false))
            .ToList();
    }

    private static void ValidateTypeSpecificRules(
        string commandType,
        Guid? targetAlarmEventUid,
        Domain.Entities.Devices.DeviceState state)
    {
        switch (commandType)
        {
            case DbStatusCodes.CommandType.MuteAlarm:
            {
                var reject = OwnerDeviceCommandAcceptanceRules.EvaluateMuteRule(
                    targetAlarmEventUid,
                    state.CurrentAlarmEventUid);
                if (reject == ExecuteCommandDraft.RejectCodes.MuteTargetRequired)
                {
                    throw new BadRequestException(
                        "معرف الحادثة الجارية مطلوب لكتم الإنذار.",
                        reject);
                }

                if (reject is not null)
                {
                    throw new ConflictException(
                        "معرف الحادثة لا يطابق الحادثة الجارية على الجهاز.",
                        reject);
                }

                break;
            }

            case DbStatusCodes.CommandType.TestSiren:
            {
                var reject = OwnerDeviceCommandAcceptanceRules.EvaluateTestSirenRule(
                    state.SirenState,
                    state.CurrentAlarmEventUid);
                if (reject is not null)
                {
                    throw new ConflictException(
                        "يُرفض اختبار الصفارة أثناء إنذار جارٍ.",
                        reject);
                }

                break;
            }

            case DbStatusCodes.CommandType.StopTest:
            {
                var reject = OwnerDeviceCommandAcceptanceRules.EvaluateStopTestRule(state.SirenState);
                if (reject is not null)
                {
                    throw new ConflictException(
                        "لا يوجد اختبار صفارة جارٍ لإيقافه.",
                        reject);
                }

                break;
            }
        }
    }

    private static OwnerDeviceCommandResponseDto MapReplayOrConflict(
        DeviceCommand existing,
        Guid ownerUserId,
        Guid deviceId,
        string commandType,
        Guid? targetAlarmEventUid)
    {
        if (!OwnerDeviceCommandAcceptanceRules.MatchesIdempotentReplay(
                existing.RequestedByUserId,
                ownerUserId,
                existing.DeviceId,
                deviceId,
                existing.CommandType,
                commandType,
                existing.TargetAlarmEventUid,
                targetAlarmEventUid))
        {
            throw new ConflictException(
                existing.RequestedByUserId != ownerUserId
                    ? "تعارض مفتاح Idempotency."
                    : "أُعيد استخدام مفتاح Idempotency بطلب مختلف.",
                ExecuteCommandDraft.RejectCodes.IdempotencyConflict);
        }

        return Map(existing, confirmedArmState: null, confirmedSirenState: null, idempotentReplay: true);
    }

    private static OwnerDeviceCommandResponseDto Map(
        DeviceCommand command,
        string? confirmedArmState,
        string? confirmedSirenState,
        bool idempotentReplay)
        => new()
        {
            CommandId = command.Id,
            DeviceId = command.DeviceId,
            Type = command.CommandType,
            Status = command.Status,
            CommandSequence = command.CommandSequence,
            OwnershipVersion = command.OwnershipVersion,
            ObservedStateSequence = command.ObservedStateSequence,
            CreatedAt = command.CreatedAt,
            ExpiresAt = command.ExpiresAt,
            TargetAlarmEventUid = command.TargetAlarmEventUid,
            RejectCode = command.RejectCode,
            ResultStateSequence = command.ResultStateSequence,
            SentAt = command.SentAt,
            AcknowledgedAt = command.AcknowledgedAt,
            ReconciledAt = command.ReconciledAt,
            IdempotentReplay = idempotentReplay,
            ActualStateChanged = false,
            ConfirmedArmState = confirmedArmState,
            ConfirmedSirenState = confirmedSirenState
        };

    private static string BuildExecuteCommandPayload(DeviceCommand command, Guid messageId)
    {
        // Draft shape from development-plan §4.4 — not a G1-adopted production contract.
        var payload = new Dictionary<string, object?>
        {
            ["schemaVersion"] = ExecuteCommandDraft.SchemaVersion,
            ["deviceId"] = command.DeviceId,
            ["messageId"] = messageId,
            ["messageType"] = ExecuteCommandDraft.MqttMessageType,
            ["commandId"] = command.Id,
            ["commandSequence"] = command.CommandSequence,
            ["ownershipVersion"] = command.OwnershipVersion,
            ["type"] = command.CommandType,
            ["expiresAt"] = command.ExpiresAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
        };

        if (command.CommandType == DbStatusCodes.CommandType.MuteAlarm)
            payload["targetAlarmEventUid"] = command.TargetAlarmEventUid;

        return JsonSerializer.Serialize(payload);
    }

    private static bool IsPendingConflict(DbUpdateException ex)
    {
        var text = ex.InnerException?.Message ?? ex.Message;
        return text.Contains("ux_device_pending_command", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIdempotencyConflict(DbUpdateException ex)
    {
        var text = ex.InnerException?.Message ?? ex.Message;
        return text.Contains("ux_device_commands_idempotency", StringComparison.OrdinalIgnoreCase);
    }
}
