using System.Text.Json;
using Application.Dtos.MqttDtos;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Events;
using Domain.Entities.Notifications;
using Domain.Entities.Operations;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.MqttServices;

/// <summary>
/// Persists draft ALARM_EVENT (with optional ACTIVE-owner notification + EVENT_PERSISTED outbox),
/// EVENT_LOSS_REPORT, and DEVICE_FAULT under PostgreSQL uniqueness and row locks (T-06).
/// </summary>
public sealed class AlarmEventIngestService : IAlarmEventIngestService
{
    private static readonly HashSet<string> FaultStatuses = new(StringComparer.Ordinal)
    {
        DbStatusCodes.Fault.Active,
        DbStatusCodes.Fault.Resolved
    };

    private readonly IDeviceRepository _devices;
    private readonly IAlarmEventRepository _alarms;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;

    public AlarmEventIngestService(
        IDeviceRepository devices,
        IAlarmEventRepository alarms,
        IUnitOfWork uow,
        IAuditLogService audit)
    {
        _devices = devices;
        _alarms = alarms;
        _uow = uow;
        _audit = audit;
    }

    public async Task<AlarmEventIngestResultDto> IngestAsync(
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

        ParsedEventMessage parsed;
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

            // Serialize concurrent alarm/ownership writers for this device (same lock path as T-04/T-05).
            var state = await _devices.GetStateForUpdateAsync(parsed.DeviceId, ct);
            if (state is null)
            {
                await tx.RollbackAsync(ct);
                return Reject(parsed.DeviceId, parsed.MessageType, "DEVICE_STATE_MISSING", "حالة الجهاز غير مهيأة.");
            }

            var now = DateTimeOffset.UtcNow;
            state.LastSeenAt = now;

            AlarmEventIngestResultDto result = parsed.MessageType switch
            {
                AlarmEventDraft.MessageTypes.AlarmEvent
                    => await PersistAlarmEventAsync(parsed, now, ct),
                AlarmEventDraft.MessageTypes.EventLossReport
                    => await PersistEventLossAsync(parsed, now, ct),
                AlarmEventDraft.MessageTypes.DeviceFault
                    => await PersistDeviceFaultAsync(parsed, now, ct),
                _ => Reject(parsed.DeviceId, parsed.MessageType, "UNSUPPORTED_MESSAGE_TYPE", "نوع الرسالة غير مدعوم.")
            };

            if (result.Outcome == AlarmEventDraft.IngestOutcomes.Rejected)
            {
                await tx.RollbackAsync(ct);
                return result;
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateException ex) when (IsAlarmEventUidConflict(ex) && parsed.MessageType == AlarmEventDraft.MessageTypes.AlarmEvent)
        {
            await tx.RollbackAsync(ct);
            _alarms.ClearChangeTracker();
            // Concurrent first insert lost the unique race — confirm the winner and queue EVENT_PERSISTED.
            return await ConfirmDuplicateAfterConflictAsync(parsed, ct);
        }
        catch (DbUpdateException ex) when (IsEventLossUidConflict(ex) && parsed.MessageType == AlarmEventDraft.MessageTypes.EventLossReport)
        {
            await tx.RollbackAsync(ct);
            _alarms.ClearChangeTracker();
            return await ConfirmDuplicateLossAfterConflictAsync(parsed, ct);
        }
        catch (DbUpdateException ex) when (IsDeviceFaultUidConflict(ex) && parsed.MessageType == AlarmEventDraft.MessageTypes.DeviceFault)
        {
            await tx.RollbackAsync(ct);
            _alarms.ClearChangeTracker();
            return await ConfirmDuplicateFaultAfterConflictAsync(parsed, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<AlarmEventIngestResultDto> PersistAlarmEventAsync(
        ParsedEventMessage parsed,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var eventUid = parsed.EventUid
            ?? throw new BadRequestException("معرف الحادثة مطلوب.", "MISSING_FIELD");

        var existing = await _alarms.FindByDeviceAndEventUidAsync(parsed.DeviceId, eventUid, ct);
        if (existing is not null)
            return await QueueEventPersistedForExistingAsync(existing, parsed, now, ct);

        var ownership = await _alarms.FindOwnershipByVersionForUpdateAsync(
            parsed.DeviceId,
            parsed.OwnershipVersion,
            ct);

        Guid? sensorId = null;
        if (!string.IsNullOrWhiteSpace(parsed.SensorKey))
        {
            var sensor = await _alarms.FindSensorByKeyAsync(parsed.DeviceId, parsed.SensorKey, ct);
            sensorId = sensor?.Id;
        }

        // clockTrusted=false: never treat device wall-clock as a confirmed occurrence time (FR-023).
        DateTimeOffset? occurredDeviceAt = parsed.ClockTrusted ? parsed.OccurredDeviceAt : null;
        var phase = parsed.AlarmPhase;
        var endedFirst = phase == AlarmEventDraft.AlarmPhase.Ended;

        var alarm = new AlarmEvent
        {
            Id = Guid.NewGuid(),
            DeviceId = parsed.DeviceId,
            EventUid = eventUid,
            OwnershipId = ownership?.Id,
            OwnershipVersion = parsed.OwnershipVersion,
            SensorId = sensorId,
            SensorKeySnapshot = parsed.SensorKey ?? string.Empty,
            Status = endedFirst ? DbStatusCodes.AlarmEvent.Ended : DbStatusCodes.AlarmEvent.Active,
            OccurredDeviceAt = endedFirst ? null : occurredDeviceAt,
            ReceivedAt = now,
            EndedDeviceAt = endedFirst ? occurredDeviceAt : null,
            EndedReceivedAt = endedFirst ? now : null,
            ClockTrusted = parsed.ClockTrusted,
            WasBuffered = parsed.WasBuffered,
            SignalSummary = parsed.SignalSummary
        };
        _alarms.AddAlarmEvent(alarm);

        Guid? notificationId = null;
        var notificationCreated = false;
        if (!endedFirst
            && ownership is not null
            && ownership.Status == DbStatusCodes.Ownership.Active
            && ownership.EndedAt is null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = ownership.OwnerUserId,
                NotificationType = DbStatusCodes.NotificationType.Alarm,
                AlarmEventId = alarm.Id,
                DeviceFaultId = null,
                AdminAlertId = null,
                Status = DbStatusCodes.NotificationStatus.Queued,
                CreatedAt = now
            };
            _alarms.AddNotification(notification);
            notificationId = notification.Id;
            notificationCreated = true;
        }

        var outbox = await EnsureEventPersistedOutboxAsync(alarm, phase, now, ct);

        await _audit.WriteAsync(
            actorUserId: null,
            AuditActions.AlarmEventPersisted,
            AuditActions.TargetTypes.AlarmEvent,
            alarm.Id,
            DbStatusCodes.AuditResult.Success,
            new
            {
                deviceId = alarm.DeviceId,
                eventUid = alarm.EventUid,
                ownershipId = alarm.OwnershipId,
                ownershipVersion = alarm.OwnershipVersion,
                wasBuffered = alarm.WasBuffered,
                clockTrusted = alarm.ClockTrusted,
                notificationCreated,
                duplicate = false,
                source = "MQTT_INBOUND"
            },
            alarm.DeviceId,
            ct);

        return new AlarmEventIngestResultDto
        {
            Outcome = AlarmEventDraft.IngestOutcomes.Applied,
            DeviceId = parsed.DeviceId,
            MessageType = parsed.MessageType,
            AlarmEventId = alarm.Id,
            EventUid = alarm.EventUid,
            NotificationId = notificationId,
            OutboxMessageId = outbox.Id,
            EventCreated = true,
            NotificationCreated = notificationCreated,
            EventPersistedQueued = true,
            WasBuffered = alarm.WasBuffered,
            ClockTrusted = alarm.ClockTrusted,
            ReceivedAt = alarm.ReceivedAt,
            OccurredDeviceAt = alarm.OccurredDeviceAt
        };
    }

    private async Task<AlarmEventIngestResultDto> QueueEventPersistedForExistingAsync(
        AlarmEvent existing,
        ParsedEventMessage parsed,
        DateTimeOffset now,
        CancellationToken ct)
    {
        ApplyPhaseTransition(existing, parsed, now);

        // ux_outbox_messages_type_aggregate: re-queue the same durable row so EVENT_PERSISTED is
        // re-sent after Commit without a second outbox insert (FR-021 duplicate confirm).
        var outbox = await EnsureEventPersistedOutboxAsync(existing, parsed.AlarmPhase, now, ct);

        await _audit.WriteAsync(
            actorUserId: null,
            AuditActions.AlarmEventPersisted,
            AuditActions.TargetTypes.AlarmEvent,
            existing.Id,
            DbStatusCodes.AuditResult.Success,
            new
            {
                deviceId = existing.DeviceId,
                eventUid = existing.EventUid,
                ownershipId = existing.OwnershipId,
                ownershipVersion = existing.OwnershipVersion,
                wasBuffered = existing.WasBuffered,
                clockTrusted = existing.ClockTrusted,
                notificationCreated = false,
                duplicate = true,
                source = "MQTT_INBOUND"
            },
            existing.DeviceId,
            ct);

        return new AlarmEventIngestResultDto
        {
            Outcome = AlarmEventDraft.IngestOutcomes.DuplicateConfirmed,
            DeviceId = existing.DeviceId,
            MessageType = parsed.MessageType,
            AlarmEventId = existing.Id,
            EventUid = existing.EventUid,
            OutboxMessageId = outbox.Id,
            EventCreated = false,
            NotificationCreated = false,
            EventPersistedQueued = true,
            WasBuffered = existing.WasBuffered,
            ClockTrusted = existing.ClockTrusted,
            ReceivedAt = existing.ReceivedAt,
            OccurredDeviceAt = existing.OccurredDeviceAt
        };
    }

    /// <summary>
    /// <see cref="AlarmEventDraft.AlarmPhase.Ended"/> closes the row. A later
    /// <see cref="AlarmEventDraft.AlarmPhase.Active"/> does not reopen it.
    /// Mute is not represented here.
    /// </summary>
    private static void ApplyPhaseTransition(AlarmEvent existing, ParsedEventMessage parsed, DateTimeOffset now)
    {
        if (parsed.AlarmPhase != AlarmEventDraft.AlarmPhase.Ended)
            return;
        if (existing.Status == DbStatusCodes.AlarmEvent.Ended)
            return;

        existing.Status = DbStatusCodes.AlarmEvent.Ended;
        existing.EndedReceivedAt = now;
        existing.EndedDeviceAt = parsed.ClockTrusted ? parsed.OccurredDeviceAt : null;
    }

    private async Task<OutboxMessage> EnsureEventPersistedOutboxAsync(
        AlarmEvent alarm,
        string phase,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var existingOutbox = await _alarms.FindOutboxByTypeAndAggregateForUpdateAsync(
            EventPersistedDraft.OutboxPublishMessageType,
            alarm.Id,
            ct);

        if (existingOutbox is not null)
        {
            var explicitAck = EventPersistedPayload.TryReadExplicitPhase(existingOutbox.Payload, out var explicitPhase);
            var queue = EventPersistedPayload.ReadQueue(existingOutbox.Payload);
            if (existingOutbox.ProcessedAt is not null && queue.Count > 0)
                queue.RemoveAt(0);
            // An old EVENT_PERSISTED without alarmPhase confirms only the start. It must not drop ENDED.
            if (alarm.Status == DbStatusCodes.AlarmEvent.Ended
                && !queue.Contains(AlarmEventDraft.AlarmPhase.Ended)
                && !(explicitAck && explicitPhase == AlarmEventDraft.AlarmPhase.Ended && existingOutbox.ProcessedAt is not null))
            {
                queue.Add(AlarmEventDraft.AlarmPhase.Ended);
            }

            if (!queue.Contains(phase))
                queue.Add(phase);
            if (queue.Count == 0)
                queue.Add(phase);

            existingOutbox.Payload = EventPersistedPayload.Build(
                alarm.DeviceId,
                alarm.EventUid,
                queue[0],
                queue);

            if (existingOutbox.ProcessedAt is not null)
            {
                // Prior transport completion: FR-021 requires a fresh confirm cycle on redelivery.
                // Reset attempt budget so historical redeliveries cannot park via MaxAttempts forever.
                existingOutbox.ProcessedAt = null;
                existingOutbox.AvailableAt = now;
                existingOutbox.AttemptCount = 0;
                existingOutbox.LastErrorCode = null;
            }
            else if (existingOutbox.AvailableAt > now)
            {
                // Still pending but backed off after publish failure / disabled publisher:
                // device redelivery accelerates the next attempt without wiping attempt accounting.
                existingOutbox.AvailableAt = now;
            }
            // else: already due/pending — keep AttemptCount and AvailableAt; payload refresh only.
            // Concurrent worker holding this row blocks on FOR UPDATE until publish+commit finish.

            return existingOutbox;
        }

        var outbox = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = EventPersistedDraft.OutboxAggregateType,
            AggregateId = alarm.Id,
            MessageType = EventPersistedDraft.OutboxPublishMessageType,
            Payload = EventPersistedPayload.Build(alarm.DeviceId, alarm.EventUid, phase, [phase]),
            CreatedAt = now,
            AvailableAt = now,
            AttemptCount = 0
        };
        _alarms.AddOutboxMessage(outbox);
        return outbox;
    }

    private async Task<AlarmEventIngestResultDto> PersistEventLossAsync(
        ParsedEventMessage parsed,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var reportUid = parsed.ReportUid
            ?? throw new BadRequestException("معرف تقرير الفقد مطلوب.", "MISSING_FIELD");

        var existing = await _alarms.FindLossReportAsync(parsed.DeviceId, reportUid, ct);
        if (existing is not null)
        {
            return new AlarmEventIngestResultDto
            {
                Outcome = AlarmEventDraft.IngestOutcomes.DuplicateConfirmed,
                DeviceId = parsed.DeviceId,
                MessageType = parsed.MessageType,
                EventLossReportId = existing.Id,
                EventCreated = false
            };
        }

        var ownership = await _alarms.FindOwnershipByVersionForUpdateAsync(
            parsed.DeviceId,
            parsed.OwnershipVersion,
            ct);

        DateTimeOffset? deviceOccurredAt = parsed.ClockTrusted ? parsed.OccurredDeviceAt : null;

        var report = new EventLossReport
        {
            Id = Guid.NewGuid(),
            DeviceId = parsed.DeviceId,
            ReportUid = reportUid,
            OwnershipId = ownership?.Id,
            Reason = parsed.LossReason!,
            LostCount = parsed.LostCount,
            DeviceOccurredAt = deviceOccurredAt,
            ReceivedAt = now,
            OwnershipVersion = parsed.OwnershipVersion
        };
        _alarms.AddEventLossReport(report);

        await _audit.WriteAsync(
            actorUserId: null,
            AuditActions.EventLossReportPersisted,
            AuditActions.TargetTypes.EventLossReport,
            report.Id,
            DbStatusCodes.AuditResult.Success,
            new
            {
                deviceId = report.DeviceId,
                reportUid = report.ReportUid,
                reason = report.Reason,
                lostCount = report.LostCount,
                ownershipVersion = report.OwnershipVersion,
                duplicate = false,
                source = "MQTT_INBOUND"
            },
            report.DeviceId,
            ct);

        return new AlarmEventIngestResultDto
        {
            Outcome = AlarmEventDraft.IngestOutcomes.Applied,
            DeviceId = parsed.DeviceId,
            MessageType = parsed.MessageType,
            EventLossReportId = report.Id,
            EventCreated = true,
            ReceivedAt = report.ReceivedAt
        };
    }

    private async Task<AlarmEventIngestResultDto> PersistDeviceFaultAsync(
        ParsedEventMessage parsed,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var faultUid = parsed.FaultUid
            ?? throw new BadRequestException("معرف العطل مطلوب.", "MISSING_FIELD");

        var existing = await _alarms.FindFaultAsync(parsed.DeviceId, faultUid, ct);
        if (existing is not null)
        {
            return new AlarmEventIngestResultDto
            {
                Outcome = AlarmEventDraft.IngestOutcomes.DuplicateConfirmed,
                DeviceId = parsed.DeviceId,
                MessageType = parsed.MessageType,
                DeviceFaultId = existing.Id,
                EventCreated = false
            };
        }

        var ownership = await _alarms.FindOwnershipByVersionForUpdateAsync(
            parsed.DeviceId,
            parsed.OwnershipVersion,
            ct);

        Guid? sensorId = null;
        if (!string.IsNullOrWhiteSpace(parsed.SensorKey))
        {
            var sensor = await _alarms.FindSensorByKeyAsync(parsed.DeviceId, parsed.SensorKey, ct);
            sensorId = sensor?.Id;
        }

        DateTimeOffset? deviceOccurredAt = parsed.ClockTrusted ? parsed.OccurredDeviceAt : null;

        var fault = new DeviceFault
        {
            Id = Guid.NewGuid(),
            DeviceId = parsed.DeviceId,
            FaultUid = faultUid,
            OwnershipId = ownership?.Id,
            SensorId = sensorId,
            FaultCode = parsed.FaultCode!,
            Status = parsed.FaultStatus ?? DbStatusCodes.Fault.Active,
            DeviceOccurredAt = deviceOccurredAt,
            ReceivedAt = now,
            ResolvedAt = string.Equals(
                parsed.FaultStatus,
                DbStatusCodes.Fault.Resolved,
                StringComparison.Ordinal)
                ? now
                : null,
            Details = parsed.FaultDetails,
            OwnershipVersion = parsed.OwnershipVersion
        };
        _alarms.AddDeviceFault(fault);

        Guid? notificationId = null;
        var notificationCreated = false;
        // Partial FR-025: queue FAULT notification only for ACTIVE ownership (same rule as ALARM).
        if (ownership is not null
            && ownership.Status == DbStatusCodes.Ownership.Active
            && ownership.EndedAt is null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = ownership.OwnerUserId,
                NotificationType = DbStatusCodes.NotificationType.Fault,
                AlarmEventId = null,
                DeviceFaultId = fault.Id,
                AdminAlertId = null,
                Status = DbStatusCodes.NotificationStatus.Queued,
                CreatedAt = now
            };
            _alarms.AddNotification(notification);
            notificationId = notification.Id;
            notificationCreated = true;
        }

        await _audit.WriteAsync(
            actorUserId: null,
            AuditActions.DeviceFaultPersisted,
            AuditActions.TargetTypes.DeviceFault,
            fault.Id,
            DbStatusCodes.AuditResult.Success,
            new
            {
                deviceId = fault.DeviceId,
                faultUid = fault.FaultUid,
                faultCode = fault.FaultCode,
                ownershipVersion = fault.OwnershipVersion,
                notificationCreated,
                duplicate = false,
                source = "MQTT_INBOUND"
            },
            fault.DeviceId,
            ct);

        return new AlarmEventIngestResultDto
        {
            Outcome = AlarmEventDraft.IngestOutcomes.Applied,
            DeviceId = parsed.DeviceId,
            MessageType = parsed.MessageType,
            DeviceFaultId = fault.Id,
            NotificationId = notificationId,
            EventCreated = true,
            NotificationCreated = notificationCreated,
            ReceivedAt = fault.ReceivedAt,
            ClockTrusted = parsed.ClockTrusted,
            OccurredDeviceAt = fault.DeviceOccurredAt
        };
    }

    private async Task<AlarmEventIngestResultDto> ConfirmDuplicateAfterConflictAsync(
        ParsedEventMessage parsed,
        CancellationToken ct)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            _ = await _devices.GetStateForUpdateAsync(parsed.DeviceId, ct);
            var existing = await _alarms.FindByDeviceAndEventUidAsync(
                parsed.DeviceId,
                parsed.EventUid!.Value,
                ct);
            if (existing is null)
            {
                await tx.RollbackAsync(ct);
                return Reject(
                    parsed.DeviceId,
                    parsed.MessageType,
                    "ALARM_EVENT_CONFLICT",
                    "تعارض تفرد الحادثة دون صف محفوظ.");
            }

            var now = DateTimeOffset.UtcNow;
            var result = await QueueEventPersistedForExistingAsync(existing, parsed, now, ct);
            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<AlarmEventIngestResultDto> ConfirmDuplicateLossAfterConflictAsync(
        ParsedEventMessage parsed,
        CancellationToken ct)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var existing = await _alarms.FindLossReportAsync(parsed.DeviceId, parsed.ReportUid!.Value, ct);
            if (existing is null)
            {
                await tx.RollbackAsync(ct);
                return Reject(parsed.DeviceId, parsed.MessageType, "EVENT_LOSS_CONFLICT", "تعارض تفرد تقرير الفقد.");
            }

            await tx.CommitAsync(ct);
            return new AlarmEventIngestResultDto
            {
                Outcome = AlarmEventDraft.IngestOutcomes.DuplicateConfirmed,
                DeviceId = parsed.DeviceId,
                MessageType = parsed.MessageType,
                EventLossReportId = existing.Id
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<AlarmEventIngestResultDto> ConfirmDuplicateFaultAfterConflictAsync(
        ParsedEventMessage parsed,
        CancellationToken ct)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var existing = await _alarms.FindFaultAsync(parsed.DeviceId, parsed.FaultUid!.Value, ct);
            if (existing is null)
            {
                await tx.RollbackAsync(ct);
                return Reject(parsed.DeviceId, parsed.MessageType, "DEVICE_FAULT_CONFLICT", "تعارض تفرد العطل.");
            }

            await tx.CommitAsync(ct);
            return new AlarmEventIngestResultDto
            {
                Outcome = AlarmEventDraft.IngestOutcomes.DuplicateConfirmed,
                DeviceId = parsed.DeviceId,
                MessageType = parsed.MessageType,
                DeviceFaultId = existing.Id
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static ParsedEventMessage ParseAndValidate(string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("حمولة MQTT يجب أن تكون كائناً JSON.", "INVALID_PAYLOAD");

        var schemaVersion = RequireInt32(root, "schemaVersion");
        if (schemaVersion != AlarmEventDraft.SchemaVersion)
            throw new BadRequestException("إصدار المخطط غير مدعوم.", "UNSUPPORTED_SCHEMA_VERSION");

        var messageType = RequireString(root, "messageType");
        var deviceId = RequireGuid(root, "deviceId");
        _ = RequireGuid(root, "messageId");
        _ = RequireString(root, "bootId");

        return messageType switch
        {
            AlarmEventDraft.MessageTypes.AlarmEvent => ParseAlarmEvent(root, deviceId, messageType),
            AlarmEventDraft.MessageTypes.EventLossReport => ParseEventLoss(root, deviceId, messageType),
            AlarmEventDraft.MessageTypes.DeviceFault => ParseDeviceFault(root, deviceId, messageType),
            _ => throw new BadRequestException("نوع الرسالة غير مدعوم في مسار الأحداث.", "UNSUPPORTED_MESSAGE_TYPE")
        };
    }

    private static ParsedEventMessage ParseAlarmEvent(JsonElement root, Guid deviceId, string messageType)
    {
        var eventUid = RequireGuid(root, "eventUid");
        var ownershipVersion = RequireInt64(root, "ownershipVersion");
        if (ownershipVersion < 0)
            throw new BadRequestException("إصدار الملكية غير صالح.", "INVALID_OWNERSHIP_VERSION");

        var sensorKey = RequireString(root, "sensorKey");
        var wasBuffered = RequireBoolean(root, "wasBuffered");
        var clockTrusted = RequireBoolean(root, "clockTrusted");
        var occurred = OptionalDateTimeOffset(root, "occurredDeviceAt");

        return new ParsedEventMessage
        {
            DeviceId = deviceId,
            MessageType = messageType,
            EventUid = eventUid,
            OwnershipVersion = ownershipVersion,
            SensorKey = sensorKey,
            WasBuffered = wasBuffered,
            ClockTrusted = clockTrusted,
            OccurredDeviceAt = occurred,
            SignalSummary = OptionalJsonObjectString(root, "signalSummary"),
            AlarmPhase = ParseAlarmPhase(root)
        };
    }

    /// <summary>
    /// Absent <c>alarmPhase</c> stays <see cref="AlarmEventDraft.AlarmPhase.Active"/> so older devices remain valid.
    /// </summary>
    private static string ParseAlarmPhase(JsonElement root)
    {
        if (!root.TryGetProperty("alarmPhase", out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return AlarmEventDraft.AlarmPhase.Active;
        if (el.ValueKind != JsonValueKind.String || !EventPersistedPayload.IsPhase(el.GetString()))
            throw new BadRequestException("مرحلة الحادثة غير صالحة.", "INVALID_ALARM_PHASE");
        return el.GetString()!;
    }

    private static ParsedEventMessage ParseEventLoss(JsonElement root, Guid deviceId, string messageType)
    {
        var reportUid = RequireGuid(root, "reportUid");
        var ownershipVersion = RequireInt64(root, "ownershipVersion");
        if (ownershipVersion < 0)
            throw new BadRequestException("إصدار الملكية غير صالح.", "INVALID_OWNERSHIP_VERSION");

        var reason = RequireString(root, "reason");
        if (!string.Equals(reason, DbStatusCodes.EventLossReason.BufferFull, StringComparison.Ordinal))
            throw new BadRequestException("سبب فقد الأحداث غير معتمد.", "INVALID_LOSS_REASON");

        var lostCount = OptionalInt32(root, "lostCount");
        if (lostCount is < 0)
            throw new BadRequestException("عدد الأحداث المفقودة غير صالح.", "INVALID_LOST_COUNT");

        var clockTrusted = OptionalBoolean(root, "clockTrusted") ?? false;

        return new ParsedEventMessage
        {
            DeviceId = deviceId,
            MessageType = messageType,
            ReportUid = reportUid,
            OwnershipVersion = ownershipVersion,
            LossReason = reason,
            LostCount = lostCount,
            ClockTrusted = clockTrusted,
            OccurredDeviceAt = OptionalDateTimeOffset(root, "deviceOccurredAt")
        };
    }

    private static ParsedEventMessage ParseDeviceFault(JsonElement root, Guid deviceId, string messageType)
    {
        var faultUid = RequireGuid(root, "faultUid");
        var ownershipVersion = RequireInt64(root, "ownershipVersion");
        if (ownershipVersion < 0)
            throw new BadRequestException("إصدار الملكية غير صالح.", "INVALID_OWNERSHIP_VERSION");

        var faultCode = RequireString(root, "faultCode");
        var status = OptionalString(root, "status") ?? DbStatusCodes.Fault.Active;
        if (!FaultStatuses.Contains(status))
            throw new BadRequestException("حالة العطل غير صالحة.", "INVALID_FAULT_STATUS");

        var clockTrusted = OptionalBoolean(root, "clockTrusted") ?? false;

        return new ParsedEventMessage
        {
            DeviceId = deviceId,
            MessageType = messageType,
            FaultUid = faultUid,
            OwnershipVersion = ownershipVersion,
            FaultCode = faultCode,
            FaultStatus = status,
            SensorKey = OptionalString(root, "sensorKey"),
            ClockTrusted = clockTrusted,
            OccurredDeviceAt = OptionalDateTimeOffset(root, "deviceOccurredAt"),
            FaultDetails = OptionalJsonObjectString(root, "details")
        };
    }

    private static AlarmEventIngestResultDto Reject(
        Guid deviceId,
        string? messageType,
        string code,
        string message)
        => new()
        {
            DeviceId = deviceId,
            MessageType = messageType,
            Outcome = AlarmEventDraft.IngestOutcomes.Rejected,
            RejectionCode = code,
            RejectionMessage = message
        };

    private static bool IsAlarmEventUidConflict(DbUpdateException ex)
    {
        var text = ex.InnerException?.Message ?? ex.Message;
        return text.Contains("ux_alarm_event_uid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEventLossUidConflict(DbUpdateException ex)
    {
        var text = ex.InnerException?.Message ?? ex.Message;
        return text.Contains("ux_event_loss_reports_uid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDeviceFaultUidConflict(DbUpdateException ex)
    {
        var text = ex.InnerException?.Message ?? ex.Message;
        return text.Contains("ux_device_faults_fault_uid", StringComparison.OrdinalIgnoreCase);
    }

    private static string RequireString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            throw new BadRequestException($"الحقل {name} مطلوب.", "MISSING_FIELD");
        var value = el.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new BadRequestException($"الحقل {name} مطلوب.", "MISSING_FIELD");
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

    private static int? OptionalInt32(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        if (!el.TryGetInt32(out var value))
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

    private static string? OptionalJsonObjectString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        if (el.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            throw new BadRequestException($"الحقل {name} غير صالح.", "INVALID_JSON");
        return el.GetRawText();
    }

    private sealed class ParsedEventMessage
    {
        public Guid DeviceId { get; init; }
        public string MessageType { get; init; } = string.Empty;
        public Guid? EventUid { get; init; }
        public Guid? ReportUid { get; init; }
        public Guid? FaultUid { get; init; }
        public long OwnershipVersion { get; init; }
        public string? SensorKey { get; init; }
        public bool WasBuffered { get; init; }
        public bool ClockTrusted { get; init; }
        public DateTimeOffset? OccurredDeviceAt { get; init; }
        public string? SignalSummary { get; init; }
        public string? LossReason { get; init; }
        public int? LostCount { get; init; }
        public string? FaultCode { get; init; }
        public string? FaultStatus { get; init; }
        public string? FaultDetails { get; init; }
        public string AlarmPhase { get; init; } = AlarmEventDraft.AlarmPhase.Active;
    }
}
