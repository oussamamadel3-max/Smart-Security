using Application.Dtos.FcmDtos;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Options;
using Domain.Constants;
using Domain.Entities.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.NotificationServices;

/// <summary>
/// FCM dispatch worker core (T-09). Provider ACCEPTED ≠ device delivery or user-read.
/// After the message leaves to Firebase, delivery to the handset is outside DB guarantees.
/// </summary>
public sealed class NotificationDispatchService : INotificationDispatchService
{
    private readonly INotificationDispatchRepository _queue;
    private readonly IPushTokenRepository _tokens;
    private readonly IPushTokenCrypto _crypto;
    private readonly INotificationRecipientGuard _guard;
    private readonly IFcmProvider _fcm;
    private readonly IUnitOfWork _uow;
    private readonly IOptionsMonitor<FcmOptions> _options;
    private readonly ILogger<NotificationDispatchService> _logger;

    public NotificationDispatchService(
        INotificationDispatchRepository queue,
        IPushTokenRepository tokens,
        IPushTokenCrypto crypto,
        INotificationRecipientGuard guard,
        IFcmProvider fcm,
        IUnitOfWork uow,
        IOptionsMonitor<FcmOptions> options,
        ILogger<NotificationDispatchService> logger)
    {
        _queue = queue;
        _tokens = tokens;
        _crypto = crypto;
        _guard = guard;
        _fcm = fcm;
        _uow = uow;
        _options = options;
        _logger = logger;
    }

    public async Task<int> DispatchPendingAsync(CancellationToken ct = default)
    {
        var opts = _options.CurrentValue;
        var batchSize = Math.Clamp(opts.BatchSize, 1, 100);
        var now = DateTimeOffset.UtcNow;

        IReadOnlyList<Notification> claimed;
        await using (var claimTx = await _uow.BeginTransactionAsync(ct))
        {
            try
            {
                claimed = await _queue.ClaimDueBatchAsync(now, batchSize, ct);
                await _uow.SaveChangesAsync(ct);
                await claimTx.CommitAsync(ct);
            }
            catch
            {
                await claimTx.RollbackAsync(ct);
                throw;
            }
        }

        foreach (var item in claimed)
        {
            try
            {
                await ProcessOneAsync(item.Id, opts, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "FCM dispatch failed for notification {NotificationId}. Row remains SENDING for retry.",
                    item.Id);
                await ScheduleRetryAfterCrashAsync(item.Id, opts, ct);
            }
        }

        return claimed.Count;
    }

    private async Task ProcessOneAsync(Guid notificationId, FcmOptions opts, CancellationToken ct)
    {
        var eligible = await _guard.EnsureRecipientStillEligibleAsync(notificationId, ct);
        if (!eligible)
            return;

        Notification notification;
        IReadOnlyList<PushToken> activeTokens;
        FcmDataPayload payload;

        await using (var prepTx = await _uow.BeginTransactionAsync(ct))
        {
            try
            {
                notification = await _queue.GetForUpdateAsync(notificationId, ct)
                    ?? throw new InvalidOperationException($"Notification {notificationId} missing after claim.");

                if (notification.Status == DbStatusCodes.NotificationStatus.Cancelled)
                {
                    await prepTx.CommitAsync(ct);
                    return;
                }

                if (notification.Status is DbStatusCodes.NotificationStatus.Accepted
                    or DbStatusCodes.NotificationStatus.Failed)
                {
                    await prepTx.CommitAsync(ct);
                    return;
                }

                activeTokens = await _tokens.ListActiveForUserAsync(notification.RecipientUserId, ct);
                var source = await _queue.ResolveSourceIdsAsync(notification, ct);
                payload = FcmPayloadBuilder.Build(
                    notification,
                    source.DeviceId,
                    source.AlarmEventId ?? notification.AlarmEventId,
                    source.DeviceFaultId ?? notification.DeviceFaultId,
                    source.AdminAlertId ?? notification.AdminAlertId);

                await prepTx.CommitAsync(ct);
            }
            catch
            {
                await prepTx.RollbackAsync(ct);
                throw;
            }
        }

        if (activeTokens.Count == 0)
        {
            await ParkWithoutProviderAttemptAsync(notificationId, opts, "NO_ACTIVE_TOKENS", ct);
            return;
        }

        var attemptAt = DateTimeOffset.UtcNow;
        var providerResults = new List<(PushToken Token, FcmSendResult Result)>(activeTokens.Count);
        var undecryptableTokenIds = new List<Guid>();

        foreach (var token in activeTokens)
        {
            string raw;
            try
            {
                raw = _crypto.Decrypt(token.TokenCiphertext);
            }
            catch (Exception ex)
            {
                // Decrypt failure is not a provider attempt — never invent ACCEPTED/FAILED/last_attempt_at.
                _logger.LogWarning(
                    ex,
                    "Failed to decrypt push token {PushTokenId}; revoking without provider call.",
                    token.Id);
                undecryptableTokenIds.Add(token.Id);
                continue;
            }

            var sendResult = await _fcm.SendAsync(
                new FcmSendRequest
                {
                    NotificationId = notificationId,
                    PushTokenId = token.Id,
                    RawToken = raw,
                    Payload = payload
                },
                ct);

            providerResults.Add((token, sendResult));
        }

        if (providerResults.Count == 0)
        {
            if (undecryptableTokenIds.Count > 0)
            {
                await using var revokeTx = await _uow.BeginTransactionAsync(ct);
                try
                {
                    await _tokens.RevokeActiveByIdsAsync(undecryptableTokenIds, attemptAt, ct);
                    await revokeTx.CommitAsync(ct);
                }
                catch
                {
                    await revokeTx.RollbackAsync(ct);
                    throw;
                }
            }

            await ParkWithoutProviderAttemptAsync(notificationId, opts, "NO_DECRYPTABLE_TOKENS", ct);
            return;
        }

        await using var finalizeTx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var row = await _queue.GetForUpdateAsync(notificationId, ct);
            if (row is null)
            {
                await finalizeTx.CommitAsync(ct);
                return;
            }

            // Ownership transfer may have cancelled while provider was in-flight.
            // Never overwrite CANCELLED; in-flight provider ACCEPTED cannot be unsent (SRS FR-024).
            var alreadyCancelled = row.Status == DbStatusCodes.NotificationStatus.Cancelled;

            var anyAccepted = false;
            var anyRetryable = false;

            if (undecryptableTokenIds.Count > 0)
                await _tokens.RevokeActiveByIdsAsync(undecryptableTokenIds, attemptAt, ct);

            foreach (var (token, result) in providerResults)
            {
                var nextAttempt = (short)(await _queue.GetMaxAttemptNoAsync(notificationId, token.Id, ct) + 1);
                _queue.AddDelivery(new NotificationDelivery
                {
                    NotificationId = notificationId,
                    PushTokenId = token.Id,
                    AttemptNo = nextAttempt,
                    AttemptedAt = attemptAt,
                    ProviderResult = result.ProviderResult,
                    ProviderMessageId = Truncate(result.ProviderMessageId, 255),
                    ErrorCode = Truncate(result.ErrorCode, 80)
                });

                if (result.RevokeToken && token.RevokedAt is null)
                    token.RevokedAt = attemptAt;

                if (result.ProviderResult == DbStatusCodes.ProviderResult.Accepted)
                {
                    anyAccepted = true;
                    token.LastUsedAt = attemptAt;
                }
                else if (result.ProviderResult == DbStatusCodes.ProviderResult.Retryable)
                {
                    anyRetryable = true;
                }
            }

            // Only after documented provider SendAsync calls.
            row.LastAttemptAt = attemptAt;

            if (!alreadyCancelled
                && row.Status is DbStatusCodes.NotificationStatus.Queued
                    or DbStatusCodes.NotificationStatus.Sending)
            {
                var priorAttempts = await _queue.CountProviderAttemptsAsync(notificationId, ct);
                // Count includes rows we are about to save only after SaveChanges;
                // approximate with prior + this round of provider calls only.
                var attemptTotal = priorAttempts + providerResults.Count;

                if (anyAccepted)
                {
                    row.Status = DbStatusCodes.NotificationStatus.Accepted;
                    row.NextAttemptAt = null;
                }
                else if (anyRetryable && attemptTotal < Math.Max(1, opts.MaxAttempts))
                {
                    row.Status = DbStatusCodes.NotificationStatus.Queued;
                    row.NextAttemptAt = attemptAt.AddSeconds(Math.Max(1, opts.RetryDelaySeconds));
                }
                else
                {
                    row.Status = DbStatusCodes.NotificationStatus.Failed;
                    row.NextAttemptAt = null;
                }
            }

            await _uow.SaveChangesAsync(ct);
            await finalizeTx.CommitAsync(ct);
        }
        catch
        {
            await finalizeTx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// No provider call occurred — must not set last_attempt_at or invent ACCEPTED/FAILED from FCM.
    /// Re-queue with backoff, or leave SENDING→QUEUED without claiming a send result.
    /// </summary>
    private async Task ParkWithoutProviderAttemptAsync(
        Guid notificationId,
        FcmOptions opts,
        string reasonCode,
        CancellationToken ct)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var row = await _queue.GetForUpdateAsync(notificationId, ct);
            if (row is null
                || row.Status is DbStatusCodes.NotificationStatus.Cancelled
                    or DbStatusCodes.NotificationStatus.Accepted
                    or DbStatusCodes.NotificationStatus.Failed)
            {
                await tx.CommitAsync(ct);
                return;
            }

            // Keep QUEUED for later token registration; never stamp last_attempt_at
            // and never invent ACCEPTED/FAILED without a provider call.
            _logger.LogInformation(
                "Notification {NotificationId} deferred without provider call ({Reason}).",
                notificationId,
                reasonCode);
            row.Status = DbStatusCodes.NotificationStatus.Queued;
            row.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, opts.RetryDelaySeconds));

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task ScheduleRetryAfterCrashAsync(Guid notificationId, FcmOptions opts, CancellationToken ct)
    {
        try
        {
            await using var tx = await _uow.BeginTransactionAsync(ct);
            try
            {
                var row = await _queue.GetForUpdateAsync(notificationId, ct);
                if (row is null
                    || row.Status is DbStatusCodes.NotificationStatus.Accepted
                        or DbStatusCodes.NotificationStatus.Failed
                        or DbStatusCodes.NotificationStatus.Cancelled)
                {
                    await tx.CommitAsync(ct);
                    return;
                }

                row.Status = DbStatusCodes.NotificationStatus.Queued;
                row.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, opts.RetryDelaySeconds));
                // Crash before/during provider: do not invent last_attempt_at.
                await _uow.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to re-queue notification {NotificationId} after crash.", notificationId);
        }
    }

    private static string? Truncate(string? value, int max)
    {
        if (value is null)
            return null;
        return value.Length <= max ? value : value[..max];
    }
}
