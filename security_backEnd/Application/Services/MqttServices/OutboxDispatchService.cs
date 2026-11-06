using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Options;
using Domain.Constants;
using Domain.Entities.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.MqttServices;

/// <summary>
/// Dispatches durable outbox command publishes and marks ACK timeouts.
/// Live broker publish stays behind <see cref="MqttOutboxOptions.Enabled"/> / G1.
/// </summary>
public sealed class OutboxDispatchService : IOutboxDispatchService
{
    private readonly IDeviceCommandRepository _commands;
    private readonly IDeviceRepository _devices;
    private readonly IAlarmEventRepository _alarms;
    private readonly IUnitOfWork _uow;
    private readonly IMqttOutboundPublisher _publisher;
    private readonly IOptionsMonitor<MqttOutboxOptions> _options;
    private readonly ILogger<OutboxDispatchService> _logger;

    public OutboxDispatchService(
        IDeviceCommandRepository commands,
        IDeviceRepository devices,
        IAlarmEventRepository alarms,
        IUnitOfWork uow,
        IMqttOutboundPublisher publisher,
        IOptionsMonitor<MqttOutboxOptions> options,
        ILogger<OutboxDispatchService> logger)
    {
        _commands = commands;
        _devices = devices;
        _alarms = alarms;
        _uow = uow;
        _publisher = publisher;
        _options = options;
        _logger = logger;
    }

    public async Task<int> DispatchPendingCommandsAsync(CancellationToken ct = default)
    {
        var opts = _options.CurrentValue;
        var batchSize = Math.Clamp(opts.BatchSize, 1, 100);
        var now = DateTimeOffset.UtcNow;

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var batch = await _commands.ClaimPendingPublishBatchAsync(
                ExecuteCommandDraft.OutboxPublishMessageType,
                now,
                batchSize,
                ct);

            foreach (var outbox in batch)
            {
                await ProcessOneAsync(outbox.Id, opts, ct);
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return batch.Count;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<int> DispatchPendingEventPersistedAsync(CancellationToken ct = default)
    {
        var opts = _options.CurrentValue;
        var batchSize = Math.Clamp(opts.BatchSize, 1, 100);
        var now = DateTimeOffset.UtcNow;

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var batch = await _commands.ClaimPendingPublishBatchAsync(
                EventPersistedDraft.OutboxPublishMessageType,
                now,
                batchSize,
                ct);

            foreach (var outbox in batch)
            {
                await ProcessEventPersistedAsync(outbox.Id, opts, ct);
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return batch.Count;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<int> MarkTimedOutCommandsAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var due = await _commands.ListPendingPastAckTimeoutAsync(now, limit: 50, ct);
            var marked = 0;

            foreach (var candidate in due)
            {
                var command = await _commands.GetByIdForUpdateAsync(candidate.Id, ct);
                if (command is null || command.Status != DbStatusCodes.CommandStatus.Pending)
                    continue;

                var settings = await _devices.GetSettingsAsync(command.DeviceId, ct);
                if (settings is null)
                    continue;

                var anchor = command.SentAt ?? command.CreatedAt;
                if (now < anchor.AddSeconds(settings.CommandTimeoutSeconds))
                    continue;

                var from = command.Status;
                command.Status = DbStatusCodes.CommandStatus.TimedOut;
                // TIMED_OUT is an unknown execution outcome — do not invent arm/siren changes.
                _commands.AddStatusHistory(new CommandStatusHistory
                {
                    CommandId = command.Id,
                    FromStatus = from,
                    ToStatus = DbStatusCodes.CommandStatus.TimedOut,
                    ReasonCode = ExecuteCommandDraft.HistoryReasons.TimedOut,
                    ChangedAt = now,
                    IsLateAck = false
                });
                marked++;
            }

            if (marked > 0)
                await _uow.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
            return marked;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task ProcessEventPersistedAsync(
        Guid outboxId,
        MqttOutboxOptions opts,
        CancellationToken ct)
    {
        var outbox = await _commands.GetOutboxForUpdateAsync(outboxId, ct);
        if (outbox is null || outbox.ProcessedAt is not null)
            return;

        var now = DateTimeOffset.UtcNow;
        outbox.AttemptCount += 1;

        if (!string.Equals(outbox.AggregateType, EventPersistedDraft.OutboxAggregateType, StringComparison.Ordinal))
        {
            outbox.LastErrorCode = EventPersistedDraft.RejectCodes.AggregateMissing;
            outbox.AvailableAt = now.AddSeconds(Math.Max(1, opts.RetryDelaySeconds));
            if (outbox.AttemptCount >= Math.Max(1, opts.MaxAttempts))
                outbox.ProcessedAt = now;
            return;
        }

        var alarm = await _alarms.GetByIdAsync(outbox.AggregateId, ct);
        if (alarm is null || !TryGetEventPersistedRouting(outbox.Payload, out var deviceId, out var eventUid)
            || alarm.DeviceId != deviceId
            || alarm.EventUid != eventUid)
        {
            outbox.LastErrorCode = EventPersistedDraft.RejectCodes.AggregateMissing;
            outbox.AvailableAt = now.AddSeconds(Math.Max(1, opts.RetryDelaySeconds));
            if (outbox.AttemptCount >= Math.Max(1, opts.MaxAttempts))
                outbox.ProcessedAt = now;
            return;
        }

        if (!_publisher.IsLivePublishEnabled)
        {
            outbox.LastErrorCode = EventPersistedDraft.RejectCodes.PublishDisabled;
            outbox.AvailableAt = now.AddSeconds(Math.Max(30, opts.RetryDelaySeconds));
            return;
        }

        try
        {
            var topic = EventPersistedDraft.Topics.Commands(deviceId);
            await _publisher.PublishAsync(topic, outbox.Payload, ct);
            // MQTT PUBACK ≠ proof the device dropped its local copy; only marks transport done.
            outbox.ProcessedAt = now;
            outbox.LastErrorCode = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish EVENT_PERSISTED outbox {OutboxId}", outbox.Id);
            outbox.LastErrorCode = EventPersistedDraft.RejectCodes.PublishFailed;
            outbox.AvailableAt = now.AddSeconds(Math.Max(1, opts.RetryDelaySeconds));
            if (outbox.AttemptCount >= Math.Max(1, opts.MaxAttempts))
                outbox.AvailableAt = now.AddHours(1);
        }
    }

    private static bool TryGetEventPersistedRouting(
        string payloadJson,
        out Guid deviceId,
        out Guid eventUid)
    {
        deviceId = Guid.Empty;
        eventUid = Guid.Empty;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("deviceId", out var deviceEl)
                || !root.TryGetProperty("eventUid", out var eventEl))
            {
                return false;
            }

            if (deviceEl.ValueKind == System.Text.Json.JsonValueKind.String
                && Guid.TryParse(deviceEl.GetString(), out deviceId)
                && eventEl.ValueKind == System.Text.Json.JsonValueKind.String
                && Guid.TryParse(eventEl.GetString(), out eventUid))
            {
                return true;
            }

            return deviceEl.TryGetGuid(out deviceId) && eventEl.TryGetGuid(out eventUid);
        }
        catch
        {
            return false;
        }
    }

    private async Task ProcessOneAsync(
        Guid outboxId,
        MqttOutboxOptions opts,
        CancellationToken ct)
    {
        var outbox = await _commands.GetOutboxForUpdateAsync(outboxId, ct);
        if (outbox is null || outbox.ProcessedAt is not null)
            return;

        var now = DateTimeOffset.UtcNow;
        outbox.AttemptCount += 1;

        var command = await _commands.GetByIdForUpdateAsync(outbox.AggregateId, ct);
        if (command is null)
        {
            outbox.LastErrorCode = ExecuteCommandDraft.RejectCodes.AggregateMissing;
            outbox.AvailableAt = now.AddSeconds(Math.Max(1, opts.RetryDelaySeconds));
            if (outbox.AttemptCount >= Math.Max(1, opts.MaxAttempts))
                outbox.ProcessedAt = now; // park — do not publish a missing aggregate forever
            return;
        }

        if (command.Status is DbStatusCodes.CommandStatus.Cancelled
            or DbStatusCodes.CommandStatus.Executed
            or DbStatusCodes.CommandStatus.Rejected)
        {
            // Terminal without needing publish (or already closed); mark outbox done.
            outbox.ProcessedAt = now;
            outbox.LastErrorCode = null;
            return;
        }

        if (now >= command.ExpiresAt)
        {
            outbox.LastErrorCode = ExecuteCommandDraft.RejectCodes.CommandExpired;
            outbox.ProcessedAt = now;
            return;
        }

        var state = await _devices.GetStateAsync(command.DeviceId, ct);
        if (state is null || state.ConfirmedOwnershipVersion < command.OwnershipVersion)
        {
            // Hold publish until ownership is device-confirmed (transfer / initial sync).
            outbox.LastErrorCode = ExecuteCommandDraft.RejectCodes.OwnershipVersionStale;
            outbox.AvailableAt = now.AddSeconds(Math.Max(1, opts.RetryDelaySeconds));
            return;
        }

        if (!_publisher.IsLivePublishEnabled)
        {
            // G1/ops gate: durable row stays pending for a future live publisher.
            // Tests replace IMqttOutboundPublisher with a recording enabled publisher.
            outbox.LastErrorCode = ExecuteCommandDraft.RejectCodes.PublishDisabled;
            outbox.AvailableAt = now.AddSeconds(Math.Max(30, opts.RetryDelaySeconds));
            if (outbox.AttemptCount >= Math.Max(1, opts.MaxAttempts))
            {
                _logger.LogWarning(
                    "Outbox command {CommandId} parked because live MQTT publish is disabled (G1/ops).",
                    command.Id);
            }

            return;
        }

        try
        {
            var topic = ExecuteCommandDraft.Topics.Commands(command.DeviceId);
            await _publisher.PublishAsync(topic, outbox.Payload, ct);

            // PUBACK / transport success is NOT device execution — only marks sent for ACK wait.
            if (command.SentAt is null)
                command.SentAt = now;

            outbox.ProcessedAt = now;
            outbox.LastErrorCode = null;

            _commands.AddStatusHistory(new CommandStatusHistory
            {
                CommandId = command.Id,
                FromStatus = command.Status,
                ToStatus = command.Status,
                ReasonCode = ExecuteCommandDraft.HistoryReasons.Published,
                ChangedAt = now,
                IsLateAck = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish outbox {OutboxId} for command {CommandId}", outbox.Id, command.Id);
            outbox.LastErrorCode = ExecuteCommandDraft.RejectCodes.PublishFailed;
            outbox.AvailableAt = now.AddSeconds(Math.Max(1, opts.RetryDelaySeconds));
            if (outbox.AttemptCount >= Math.Max(1, opts.MaxAttempts))
            {
                // Keep row unprocessed so ops can inspect; stop tight retry loop via far AvailableAt.
                outbox.AvailableAt = now.AddHours(1);
            }
        }
    }
}
