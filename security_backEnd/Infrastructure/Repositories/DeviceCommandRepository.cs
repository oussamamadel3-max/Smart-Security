using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities.Commands;
using Domain.Entities.Operations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class DeviceCommandRepository : IDeviceCommandRepository
{
    private readonly AppDbContext _db;

    public DeviceCommandRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<DeviceCommand?> GetByIdAsync(Guid commandId, CancellationToken ct = default)
        => _db.DeviceCommands.SingleOrDefaultAsync(c => c.Id == commandId, ct);

    public async Task<DeviceCommand?> GetByIdForUpdateAsync(Guid commandId, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM device_commands WHERE id = {commandId} FOR UPDATE",
            ct);

        return await _db.DeviceCommands.SingleOrDefaultAsync(c => c.Id == commandId, ct);
    }

    public Task<DeviceCommand?> FindByIdempotencyAsync(
        Guid requestedByUserId,
        Guid idempotencyKey,
        CancellationToken ct = default)
        => _db.DeviceCommands.SingleOrDefaultAsync(
            c => c.RequestedByUserId == requestedByUserId && c.IdempotencyKey == idempotencyKey,
            ct);

    public async Task<long> GetMaxCommandSequenceAsync(Guid deviceId, CancellationToken ct = default)
    {
        var max = await _db.DeviceCommands
            .Where(c => c.DeviceId == deviceId)
            .Select(c => (long?)c.CommandSequence)
            .MaxAsync(ct);
        return max ?? 0;
    }

    public Task<bool> HasBlockingUnresolvedCommandAsync(Guid deviceId, CancellationToken ct = default)
        => _db.DeviceCommands.AnyAsync(
            c => c.DeviceId == deviceId
                 && (c.Status == DbStatusCodes.CommandStatus.Pending
                     || (c.Status == DbStatusCodes.CommandStatus.TimedOut && c.ReconciledAt == null)),
            ct);

    public async Task<IReadOnlyList<DeviceCommand>> ListPendingPastAckTimeoutAsync(
        DateTimeOffset utcNow,
        int limit,
        CancellationToken ct = default)
    {
        // Join settings for per-device command_timeout_seconds; candidate filter then exact check in service.
        var candidates = await (
            from command in _db.DeviceCommands
            join settings in _db.DeviceSettings on command.DeviceId equals settings.DeviceId
            where command.Status == DbStatusCodes.CommandStatus.Pending
            orderby command.CreatedAt
            select new { Command = command, settings.CommandTimeoutSeconds }
        ).Take(limit * 4).ToListAsync(ct);

        return candidates
            .Where(x =>
            {
                var anchor = x.Command.SentAt ?? x.Command.CreatedAt;
                return utcNow >= anchor.AddSeconds(x.CommandTimeoutSeconds);
            })
            .Select(x => x.Command)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<DeviceCommand>> ListUnreconciledTimedOutAsync(
        Guid deviceId,
        CancellationToken ct = default)
    {
        return await _db.DeviceCommands
            .Where(c => c.DeviceId == deviceId
                        && c.Status == DbStatusCodes.CommandStatus.TimedOut
                        && c.ReconciledAt == null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public void AddCommand(DeviceCommand command)
        => _db.DeviceCommands.Add(command);

    public void AddStatusHistory(CommandStatusHistory history)
        => _db.CommandStatusHistory.Add(history);

    public void AddOutboxMessage(OutboxMessage message)
        => _db.OutboxMessages.Add(message);

    public async Task<OutboxMessage?> GetOutboxForUpdateAsync(Guid outboxId, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM outbox_messages WHERE id = {outboxId} FOR UPDATE",
            ct);

        return await _db.OutboxMessages.SingleOrDefaultAsync(m => m.Id == outboxId, ct);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingPublishBatchAsync(
        string messageType,
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken ct = default)
    {
        // Candidate ids under SKIP LOCKED; then hydrate tracked entities for the ambient transaction.
        var ids = await _db.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value"
                FROM outbox_messages
                WHERE message_type = {messageType}
                  AND processed_at IS NULL
                  AND available_at <= {utcNow}
                ORDER BY available_at, created_at
                FOR UPDATE SKIP LOCKED
                LIMIT {batchSize}
                """)
            .ToListAsync(ct);

        if (ids.Count == 0)
            return Array.Empty<OutboxMessage>();

        return await _db.OutboxMessages
            .Where(m => ids.Contains(m.Id))
            .OrderBy(m => m.AvailableAt)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }
}

