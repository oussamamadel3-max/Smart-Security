using System.Collections.Concurrent;
using Application.Dtos.FcmDtos;
using Application.Interfaces.Services;
using Application.Options;
using Domain.Constants;

namespace Infrastructure.Services.Fcm;

/// <summary>
/// Default test/dev provider. Never contacts Firebase.
/// Programmable via <see cref="SetNextResult"/> / <see cref="SetDefaultResult"/>.
/// </summary>
public sealed class FakeFcmProvider : IFcmProvider
{
    private readonly ConcurrentQueue<FcmSendResult> _scripted = new();
    private FcmSendResult _default = FcmSendResult.Accepted("fake-message-id");
    private int _callCount;

    public string ProviderName => FcmOptions.ProviderFake;

    public int CallCount => Volatile.Read(ref _callCount);

    public IList<FcmSendRequest> Sent { get; } = new List<FcmSendRequest>();

    private readonly object _sentGate = new();

    /// <summary>
    /// Optional test hook invoked after the call is counted and before a result is returned.
    /// Used to simulate ownership-transfer races during an in-flight send.
    /// </summary>
    public Func<FcmSendRequest, Task>? BeforeSendAsync { get; set; }

    public void SetDefaultResult(FcmSendResult result) => _default = result;

    public void SetNextResult(FcmSendResult result) => _scripted.Enqueue(result);

    public void EnqueueResults(params FcmSendResult[] results)
    {
        foreach (var r in results)
            _scripted.Enqueue(r);
    }

    public void Reset()
    {
        while (_scripted.TryDequeue(out _))
        {
        }

        _default = FcmSendResult.Accepted("fake-message-id");
        Interlocked.Exchange(ref _callCount, 0);
        BeforeSendAsync = null;
        lock (_sentGate)
            Sent.Clear();
    }

    public async Task<FcmSendResult> SendAsync(FcmSendRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _callCount);
        lock (_sentGate)
            Sent.Add(request);

        if (BeforeSendAsync is not null)
            await BeforeSendAsync(request).ConfigureAwait(false);

        if (!_scripted.TryDequeue(out var result))
            result = _default;

        // Never echo raw token into message ids / error codes.
        // Prefix "fake-" makes test ACCEPTED unmistakable vs Firebase message ids.
        if (result.ProviderResult == DbStatusCodes.ProviderResult.Accepted
            && string.IsNullOrWhiteSpace(result.ProviderMessageId))
        {
            result = FcmSendResult.Accepted($"fake-{request.NotificationId:N}-{request.PushTokenId:N}");
        }
        else if (result.ProviderResult == DbStatusCodes.ProviderResult.Accepted
                 && result.ProviderMessageId is not null
                 && !result.ProviderMessageId.StartsWith("fake-", StringComparison.Ordinal))
        {
            result = FcmSendResult.Accepted($"fake-{result.ProviderMessageId}");
        }

        return result;
    }
}
