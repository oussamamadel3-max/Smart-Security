using System.Text.Json;
using Application.Dtos.MqttDtos;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Commands;
using Domain.Entities.Devices;
using Domain.Exceptions;

namespace Application.Services.MqttServices;

/// <summary>
/// Validates draft inbound MQTT payloads and applies confirmed device state with row-lock sequencing.
/// COMMAND_ACK also closes the matching command row (T-05); HTTP/PUBACK alone never means execution.
/// </summary>
public sealed class DeviceInboundTelemetryService : IDeviceInboundTelemetryService
{
    private static readonly HashSet<string> ArmStates = new(StringComparer.Ordinal)
    {
        DbStatusCodes.Arm.Armed,
        DbStatusCodes.Arm.Disarmed,
        DbStatusCodes.Arm.Unknown
    };

    private static readonly HashSet<string> SirenStates = new(StringComparer.Ordinal)
    {
        DbStatusCodes.Siren.Off,
        DbStatusCodes.Siren.Alarm,
        DbStatusCodes.Siren.Test,
        DbStatusCodes.Siren.Unknown
    };

    private static readonly HashSet<string> HealthStates = new(StringComparer.Ordinal)
    {
        DbStatusCodes.Diagnostic.Healthy,
        DbStatusCodes.Diagnostic.Fault,
        DbStatusCodes.Diagnostic.Unknown
    };

    private static readonly HashSet<string> AckStatuses = new(StringComparer.Ordinal)
    {
        DbStatusCodes.CommandStatus.Executed,
        DbStatusCodes.CommandStatus.Rejected
    };

    private readonly IDeviceRepository _devices;
    private readonly IDeviceCommandRepository _commands;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;

    public DeviceInboundTelemetryService(
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

    public async Task<DeviceInboundIngestResultDto> IngestAsync(
        DeviceInboundEnvelope envelope,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.TopicDeviceId == Guid.Empty
            || envelope.AuthenticatedDeviceId == Guid.Empty
            || string.IsNullOrWhiteSpace(envelope.PayloadJson))
        {
            return Reject(Guid.Empty, null, "INVALID_ENVELOPE", "غلاف الرسالة غير صالح.");
        }

        if (envelope.TopicDeviceId != envelope.AuthenticatedDeviceId)
        {
            return Reject(
                envelope.AuthenticatedDeviceId,
                null,
                "DEVICE_IDENTITY_MISMATCH",
                "هوية القناة لا تطابق هوية اعتماد الجهاز.");
        }

        ParsedInboundMessage parsed;
        try
        {
            parsed = ParseAndValidate(envelope.PayloadJson);
        }
        catch (BadRequestException ex)
        {
            return Reject(envelope.AuthenticatedDeviceId, null, ex.ErrorCode ?? "INVALID_PAYLOAD", ex.Message);
        }

        if (parsed.DeviceId != envelope.AuthenticatedDeviceId
            || parsed.DeviceId != envelope.TopicDeviceId)
        {
            return Reject(
                envelope.AuthenticatedDeviceId,
                parsed.MessageType,
                "DEVICE_IDENTITY_MISMATCH",
                "معرف الجهاز في الحمولة لا يطابق الهوية المصرح بها.");
        }

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var device = await _devices.GetByIdAsync(parsed.DeviceId, ct);
            if (device is null)
            {
                await tx.RollbackAsync(ct);
                return Reject(parsed.DeviceId, parsed.MessageType, "DEVICE_NOT_FOUND", "الجهاز غير موجود.");
            }

            if (device.LifecycleStatus == DbStatusCodes.DeviceLifecycle.Retired)
            {
                await tx.RollbackAsync(ct);
                return Reject(parsed.DeviceId, parsed.MessageType, "DEVICE_RETIRED", "الجهاز مؤرشف.");
            }

            if (!await _devices.HasActiveCredentialAsync(parsed.DeviceId, ct))
            {
                await tx.RollbackAsync(ct);
                return Reject(
                    parsed.DeviceId,
                    parsed.MessageType,
                    "DEVICE_CREDENTIAL_REVOKED",
                    "لا يوجد اعتماد MQTT نشط للجهاز.");
            }

            var state = await _devices.GetStateForUpdateAsync(parsed.DeviceId, ct);
            if (state is null)
            {
                await tx.RollbackAsync(ct);
                return Reject(parsed.DeviceId, parsed.MessageType, "DEVICE_STATE_MISSING", "حالة الجهاز غير مهيأة.");
            }

            var now = DateTimeOffset.UtcNow;
            var result = new DeviceInboundIngestResultDto
            {
                DeviceId = parsed.DeviceId,
                MessageType = parsed.MessageType,
                StoredSequence = state.StateSequence,
                ObservedSequence = parsed.Sequence,
                ClockTrusted = state.ClockTrusted,
                ArmState = state.ArmState,
                SirenState = state.SirenState,
                HealthState = state.HealthState,
                LastSeenAt = state.LastSeenAt
            };

            // Any authenticated inbound message from the device refreshes last_seen_at (connectivity).
            state.LastSeenAt = now;
            result.LastSeenUpdated = true;
            result.LastSeenAt = now;

            if (parsed.MessageType == MqttInboundDraft.MessageTypes.Heartbeat)
            {
                await _uow.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                result.Outcome = MqttInboundDraft.IngestOutcomes.HeartbeatOnly;
                return result;
            }

            var sequence = parsed.Sequence
                ?? throw new BadRequestException("تسلسل الحالة مطلوب.", "STATE_SEQUENCE_REQUIRED");

            if (sequence < 0)
            {
                await tx.RollbackAsync(ct);
                return Reject(parsed.DeviceId, parsed.MessageType, "INVALID_STATE_SEQUENCE", "تسلسل الحالة غير صالح.");
            }

            if (sequence <= state.StateSequence)
            {
                // Stale/duplicate sequence must not rewind device_states.
                // A late COMMAND_ACK for an unresolved command may still close the command row
                // when resultStateSequence is not newer than the stored state (FR-015 late ack).
                var lateOnly = false;
                if (parsed.MessageType == MqttInboundDraft.MessageTypes.CommandAck
                    && parsed.CommandId is Guid lateCommandId)
                {
                    lateOnly = await TryCloseCommandFromAckAsync(
                        deviceId: parsed.DeviceId,
                        commandId: lateCommandId,
                        ackStatus: parsed.AckStatus!,
                        ownershipVersion: parsed.AckOwnershipVersion,
                        resultStateSequence: sequence,
                        rejectCode: parsed.AckRejectCode,
                        stateSequenceApplied: false,
                        now,
                        ct);
                }

                await _uow.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                result.Outcome = lateOnly
                    ? MqttInboundDraft.IngestOutcomes.CommandClosedOnly
                    : MqttInboundDraft.IngestOutcomes.IgnoredStaleSequence;
                result.CommandClosed = lateOnly;
                result.StoredSequence = state.StateSequence;
                return result;
            }

            ApplyConfirmedState(state, parsed, now);
            var ownershipActivated = await TryActivateOwnershipAsync(device, state, ct);

            var commandClosed = false;
            if (parsed.MessageType == MqttInboundDraft.MessageTypes.CommandAck
                && parsed.CommandId is Guid commandId)
            {
                commandClosed = await TryCloseCommandFromAckAsync(
                    deviceId: parsed.DeviceId,
                    commandId: commandId,
                    ackStatus: parsed.AckStatus!,
                    ownershipVersion: parsed.AckOwnershipVersion,
                    resultStateSequence: sequence,
                    rejectCode: parsed.AckRejectCode,
                    stateSequenceApplied: true,
                    now,
                    ct);
            }
            else if (parsed.MessageType == MqttInboundDraft.MessageTypes.State)
            {
                await ReconcileTimedOutCommandsAsync(parsed.DeviceId, sequence, now, ct);
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            result.Outcome = MqttInboundDraft.IngestOutcomes.Applied;
            result.StateApplied = true;
            result.OwnershipActivated = ownershipActivated;
            result.CommandClosed = commandClosed;
            result.StoredSequence = state.StateSequence;
            result.ObservedSequence = sequence;
            result.ArmState = state.ArmState;
            result.SirenState = state.SirenState;
            result.HealthState = state.HealthState;
            result.ClockTrusted = state.ClockTrusted;
            result.LastSeenAt = state.LastSeenAt;
            return result;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<bool> TryActivateOwnershipAsync(
        Device device,
        DeviceState state,
        CancellationToken ct)
    {
        var open = await _devices.GetOpenOwnershipAsync(device.Id, ct);
        if (open is null
            || open.Status != DbStatusCodes.Ownership.Pending
            || open.Version != state.ConfirmedOwnershipVersion)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var armBefore = state.ArmState;
        open.Status = DbStatusCodes.Ownership.Active;
        open.ActivatedAt = now;
        open.StartedAt = now;

        if (device.LifecycleStatus == DbStatusCodes.DeviceLifecycle.Registered)
            device.LifecycleStatus = DbStatusCodes.DeviceLifecycle.Active;

        device.UpdatedAt = now;

        await _audit.WriteAsync(
            actorUserId: null,
            AuditActions.OwnershipActivated,
            AuditActions.TargetTypes.Ownership,
            open.Id,
            DbStatusCodes.AuditResult.Success,
            new
            {
                deviceId = device.Id,
                ownershipId = open.Id,
                ownershipVersion = open.Version,
                confirmedOwnershipVersion = state.ConfirmedOwnershipVersion,
                armStateUnchanged = armBefore,
                source = "MQTT_INBOUND"
            },
            device.Id,
            ct);

        return true;
    }

    private static void ApplyConfirmedState(DeviceState state, ParsedInboundMessage parsed, DateTimeOffset receivedAt)
    {
        state.StateSequence = parsed.Sequence!.Value;
        state.LastStateAt = receivedAt;
        state.ArmState = parsed.ArmState!;
        state.SirenState = parsed.SirenState!;
        state.HealthState = parsed.HealthState!;
        state.CurrentAlarmEventUid = parsed.CurrentAlarmEventUid;
        state.ClockTrusted = parsed.ClockTrusted;

        if (parsed.ConfirmedOwnershipVersion is long confirmed
            && confirmed > state.ConfirmedOwnershipVersion)
        {
            state.ConfirmedOwnershipVersion = confirmed;
        }

        if (parsed.DeviceReportedAt is not null && parsed.ClockTrusted)
            state.DeviceReportedAt = parsed.DeviceReportedAt;
        else if (!parsed.ClockTrusted)
            state.DeviceReportedAt = null;

        if (parsed.FirmwareVersion is not null)
            state.FirmwareVersion = parsed.FirmwareVersion;

        if (parsed.AppliedSettingsVersion is long settingsVersion && settingsVersion >= 0)
            state.AppliedSettingsVersion = settingsVersion;

        if (parsed.HealthCode is not null)
            state.HealthCode = parsed.HealthCode;

        if (parsed.PowerSource is not null)
            state.PowerSource = parsed.PowerSource;

        if (parsed.BatteryPercent is not null)
            state.BatteryPercent = parsed.BatteryPercent;
    }

    private static ParsedInboundMessage ParseAndValidate(string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("حمولة MQTT يجب أن تكون كائناً JSON.", "INVALID_PAYLOAD");

        var schemaVersion = RequireInt32(root, "schemaVersion");
        if (schemaVersion != MqttInboundDraft.SchemaVersion)
            throw new BadRequestException("إصدار المخطط غير مدعوم.", "UNSUPPORTED_SCHEMA_VERSION");

        var messageType = RequireString(root, "messageType");
        var deviceId = RequireGuid(root, "deviceId");
        _ = RequireGuid(root, "messageId");
        _ = RequireString(root, "bootId");

        return messageType switch
        {
            MqttInboundDraft.MessageTypes.Heartbeat => ParseHeartbeat(root, deviceId, messageType),
            MqttInboundDraft.MessageTypes.State => ParseState(root, deviceId, messageType),
            MqttInboundDraft.MessageTypes.CommandAck => ParseCommandAck(root, deviceId, messageType),
            _ => throw new BadRequestException("نوع الرسالة غير مدعوم في هذا المسار.", "UNSUPPORTED_MESSAGE_TYPE")
        };
    }

    private static ParsedInboundMessage ParseHeartbeat(JsonElement root, Guid deviceId, string messageType)
    {
        var clockTrusted = RequireBoolean(root, "clockTrusted");
        return new ParsedInboundMessage
        {
            DeviceId = deviceId,
            MessageType = messageType,
            ClockTrusted = clockTrusted,
            Sequence = null
        };
    }

    private static ParsedInboundMessage ParseState(JsonElement root, Guid deviceId, string messageType)
    {
        var sequence = RequireInt64(root, "stateSequence");
        var arm = RequireEnum(root, "armState", ArmStates);
        var siren = RequireEnum(root, "sirenState", SirenStates);
        var health = RequireEnum(root, "healthState", HealthStates);
        var clockTrusted = RequireBoolean(root, "clockTrusted");
        var confirmedOwnership = RequireInt64(root, "confirmedOwnershipVersion");
        if (confirmedOwnership < 0)
            throw new BadRequestException("إصدار الملكية المؤكد غير صالح.", "INVALID_OWNERSHIP_VERSION");

        return new ParsedInboundMessage
        {
            DeviceId = deviceId,
            MessageType = messageType,
            Sequence = sequence,
            ArmState = arm,
            SirenState = siren,
            HealthState = health,
            ClockTrusted = clockTrusted,
            ConfirmedOwnershipVersion = confirmedOwnership,
            CurrentAlarmEventUid = OptionalGuid(root, "currentAlarmEventUid"),
            DeviceReportedAt = OptionalDateTimeOffset(root, "deviceReportedAt"),
            FirmwareVersion = OptionalString(root, "firmwareVersion"),
            AppliedSettingsVersion = OptionalInt64(root, "appliedSettingsVersion"),
            HealthCode = OptionalString(root, "healthCode"),
            PowerSource = OptionalString(root, "powerSource"),
            BatteryPercent = OptionalBatteryPercent(root)
        };
    }

    private static ParsedInboundMessage ParseCommandAck(JsonElement root, Guid deviceId, string messageType)
    {
        // D-09 principle: ACK carries full actual state + resultStateSequence + commandId.
        // Exact G1 schema remains draft; field names follow development-plan §4.4.
        var commandId = RequireGuid(root, "commandId");
        var ownershipVersion = RequireInt64(root, "ownershipVersion");
        var status = RequireString(root, "status");
        if (!AckStatuses.Contains(status))
            throw new BadRequestException("حالة تأكيد الأمر غير صالحة.", "INVALID_ACK_STATUS");

        var sequence = RequireInt64(root, "resultStateSequence");
        var arm = RequireEnum(root, "armState", ArmStates);
        var siren = RequireEnum(root, "sirenState", SirenStates);
        var health = RequireEnum(root, "healthState", HealthStates);
        var clockTrusted = OptionalBoolean(root, "clockTrusted") ?? false;

        return new ParsedInboundMessage
        {
            DeviceId = deviceId,
            MessageType = messageType,
            Sequence = sequence,
            ArmState = arm,
            SirenState = siren,
            HealthState = health,
            ClockTrusted = clockTrusted,
            ConfirmedOwnershipVersion = OptionalInt64(root, "confirmedOwnershipVersion") ?? ownershipVersion,
            CurrentAlarmEventUid = OptionalGuid(root, "currentAlarmEventUid"),
            DeviceReportedAt = OptionalDateTimeOffset(root, "deviceReportedAt"),
            FirmwareVersion = OptionalString(root, "firmwareVersion"),
            AppliedSettingsVersion = OptionalInt64(root, "appliedSettingsVersion"),
            HealthCode = OptionalString(root, "healthCode"),
            PowerSource = OptionalString(root, "powerSource"),
            BatteryPercent = OptionalBatteryPercent(root),
            CommandId = commandId,
            AckStatus = status,
            AckOwnershipVersion = ownershipVersion,
            AckRejectCode = OptionalString(root, "rejectCode")
        };
    }

    private async Task<bool> TryCloseCommandFromAckAsync(
        Guid deviceId,
        Guid commandId,
        string ackStatus,
        long? ownershipVersion,
        long resultStateSequence,
        string? rejectCode,
        bool stateSequenceApplied,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var command = await _commands.GetByIdForUpdateAsync(commandId, ct);
        if (command is null || command.DeviceId != deviceId)
            return false;

        // CANCELLED after ownership transfer must not be revived by a late ack.
        if (command.Status == DbStatusCodes.CommandStatus.Cancelled)
            return false;

        if (command.Status is DbStatusCodes.CommandStatus.Executed
            or DbStatusCodes.CommandStatus.Rejected)
        {
            // Duplicate delivery — already closed.
            return false;
        }

        if (ownershipVersion is long ov && ov != command.OwnershipVersion)
        {
            // Still close with device outcome; record mismatch for audit trail via reject code if rejected.
            rejectCode ??= "OWNERSHIP_VERSION_MISMATCH";
        }

        var from = command.Status;
        var isLate = from == DbStatusCodes.CommandStatus.TimedOut;
        command.Status = ackStatus;
        command.AcknowledgedAt = now;
        command.ResultStateSequence = resultStateSequence;
        if (string.Equals(ackStatus, DbStatusCodes.CommandStatus.Rejected, StringComparison.Ordinal))
            command.RejectCode = rejectCode ?? "DEVICE_REJECTED";
        else if (!string.IsNullOrWhiteSpace(rejectCode) && command.RejectCode is null)
            command.RejectCode = rejectCode;

        if (isLate || command.ReconciledAt is null)
            command.ReconciledAt = now;

        _commands.AddStatusHistory(new CommandStatusHistory
        {
            CommandId = command.Id,
            FromStatus = from,
            ToStatus = ackStatus,
            ReasonCode = isLate
                ? ExecuteCommandDraft.HistoryReasons.LateAck
                : ExecuteCommandDraft.HistoryReasons.DeviceAck,
            ChangedAt = now,
            IsLateAck = isLate
        });

        // stateSequenceApplied is informational for callers; device_states update happens separately.
        _ = stateSequenceApplied;
        return true;
    }

    private async Task ReconcileTimedOutCommandsAsync(
        Guid deviceId,
        long newerStateSequence,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var open = await _commands.ListUnreconciledTimedOutAsync(deviceId, ct);
        foreach (var item in open)
        {
            if (newerStateSequence <= item.ObservedStateSequence)
                continue;

            var command = await _commands.GetByIdForUpdateAsync(item.Id, ct);
            if (command is null
                || command.Status != DbStatusCodes.CommandStatus.TimedOut
                || command.ReconciledAt is not null)
            {
                continue;
            }

            if (newerStateSequence <= command.ObservedStateSequence)
                continue;

            // Reconciliation unlocks conflicting commands; it does NOT claim EXECUTED.
            command.ReconciledAt = now;
            _commands.AddStatusHistory(new CommandStatusHistory
            {
                CommandId = command.Id,
                FromStatus = DbStatusCodes.CommandStatus.TimedOut,
                ToStatus = DbStatusCodes.CommandStatus.TimedOut,
                ReasonCode = ExecuteCommandDraft.HistoryReasons.ReconciledByState,
                ChangedAt = now,
                IsLateAck = false
            });
        }
    }

    private static DeviceInboundIngestResultDto Reject(
        Guid deviceId,
        string? messageType,
        string code,
        string message)
        => new()
        {
            DeviceId = deviceId,
            MessageType = messageType,
            Outcome = MqttInboundDraft.IngestOutcomes.Rejected,
            RejectionCode = code,
            RejectionMessage = message
        };

    private static string RequireString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            throw new BadRequestException($"الحقل {name} مطلوب.", "MISSING_FIELD");
        var value = el.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new BadRequestException($"الحقل {name} مطلوب.", "MISSING_FIELD");
        return value;
    }

    private static string RequireEnum(JsonElement root, string name, HashSet<string> allowed)
    {
        var value = RequireString(root, name);
        if (!allowed.Contains(value))
            throw new BadRequestException($"قيمة {name} غير صالحة.", "INVALID_ENUM");
        return value;
    }

    private static Guid RequireGuid(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el))
            throw new BadRequestException($"الحقل {name} مطلوب.", "MISSING_FIELD");
        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var fromString))
            return fromString;
        if (el.TryGetGuid(out var guid))
            return guid;
        throw new BadRequestException($"الحقل {name} غير صالح.", "INVALID_GUID");
    }

    private static int RequireInt32(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || !el.TryGetInt32(out var value))
            throw new BadRequestException($"الحقل {name} مطلوب.", "MISSING_FIELD");
        return value;
    }

    private static long RequireInt64(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || !el.TryGetInt64(out var value))
            throw new BadRequestException($"الحقل {name} مطلوب.", "MISSING_FIELD");
        return value;
    }

    private static bool RequireBoolean(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || (el.ValueKind != JsonValueKind.True && el.ValueKind != JsonValueKind.False))
            throw new BadRequestException($"الحقل {name} مطلوب.", "MISSING_FIELD");
        return el.GetBoolean();
    }

    private static bool? OptionalBoolean(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        if (el.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new BadRequestException($"الحقل {name} غير صالح.", "INVALID_BOOLEAN");
        return el.GetBoolean();
    }

    private static Guid? OptionalGuid(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var fromString))
            return fromString;
        if (el.TryGetGuid(out var guid))
            return guid;
        throw new BadRequestException($"الحقل {name} غير صالح.", "INVALID_GUID");
    }

    private static long? OptionalInt64(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        if (!el.TryGetInt64(out var value))
            throw new BadRequestException($"الحقل {name} غير صالح.", "INVALID_NUMBER");
        return value;
    }

    private static string? OptionalString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        if (el.ValueKind != JsonValueKind.String)
            throw new BadRequestException($"الحقل {name} غير صالح.", "INVALID_STRING");
        return el.GetString();
    }

    private static DateTimeOffset? OptionalDateTimeOffset(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        if (el.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(el.GetString(), out var dto))
            return dto;
        throw new BadRequestException($"الحقل {name} غير صالح.", "INVALID_TIMESTAMP");
    }

    private static short? OptionalBatteryPercent(JsonElement root)
    {
        if (!root.TryGetProperty("batteryPercent", out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        if (!el.TryGetInt16(out var value) || value is < 0 or > 100)
            throw new BadRequestException("نسبة البطارية غير صالحة.", "INVALID_BATTERY_PERCENT");
        return value;
    }

    private sealed class ParsedInboundMessage
    {
        public Guid DeviceId { get; init; }
        public string MessageType { get; init; } = string.Empty;
        public long? Sequence { get; init; }
        public string? ArmState { get; init; }
        public string? SirenState { get; init; }
        public string? HealthState { get; init; }
        public bool ClockTrusted { get; init; }
        public long? ConfirmedOwnershipVersion { get; init; }
        public Guid? CurrentAlarmEventUid { get; init; }
        public DateTimeOffset? DeviceReportedAt { get; init; }
        public string? FirmwareVersion { get; init; }
        public long? AppliedSettingsVersion { get; init; }
        public string? HealthCode { get; init; }
        public string? PowerSource { get; init; }
        public short? BatteryPercent { get; init; }
        public Guid? CommandId { get; init; }
        public string? AckStatus { get; init; }
        public long? AckOwnershipVersion { get; init; }
        public string? AckRejectCode { get; init; }
    }
}
