using System.Collections.Concurrent;
using Application.Interfaces.Services;

namespace API.IntegrationTests.Support;

/// <summary>
/// Test double that records outbound MQTT publishes without a live broker.
/// Supports gated publish to exercise PostgreSQL FOR UPDATE races with ingest.
/// </summary>
public sealed class RecordingMqttOutboundPublisher : IMqttOutboundPublisher
{
    private readonly ConcurrentQueue<PublishedMessage> _messages = new();
    private readonly object _gateLock = new();
    private int _failNextCount;
    private TaskCompletionSource? _enteredGate;
    private TaskCompletionSource? _releaseGate;

    public bool IsLivePublishEnabled { get; set; } = true;

    public IReadOnlyList<PublishedMessage> Messages => _messages.ToArray();

    public void FailNext(int count = 1) => _failNextCount = count;

    /// <summary>
    /// Next <see cref="PublishAsync"/> signals <see cref="WaitUntilPublishEnteredAsync"/> then blocks until <see cref="ReleaseBlockedPublish"/>.
    /// </summary>
    public void BlockNextPublish()
    {
        lock (_gateLock)
        {
            _enteredGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _releaseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    public Task WaitUntilPublishEnteredAsync(CancellationToken ct = default)
    {
        TaskCompletionSource entered;
        lock (_gateLock)
        {
            entered = _enteredGate
                ?? throw new InvalidOperationException("BlockNextPublish was not called.");
        }

        return entered.Task.WaitAsync(ct);
    }

    public void ReleaseBlockedPublish()
    {
        TaskCompletionSource release;
        lock (_gateLock)
        {
            release = _releaseGate
                ?? throw new InvalidOperationException("BlockNextPublish was not called.");
        }

        release.TrySetResult();
    }

    public async Task PublishAsync(string topic, string payloadJson, CancellationToken ct = default)
    {
        TaskCompletionSource? entered;
        TaskCompletionSource? release;
        lock (_gateLock)
        {
            entered = _enteredGate;
            release = _releaseGate;
            // Keep release gate visible to ReleaseBlockedPublish until the waiter finishes.
            _enteredGate = null;
        }

        if (entered is not null && release is not null)
        {
            entered.TrySetResult();
            try
            {
                await release.Task.WaitAsync(ct);
            }
            finally
            {
                lock (_gateLock)
                {
                    if (ReferenceEquals(_releaseGate, release))
                        _releaseGate = null;
                }
            }
        }

        if (_failNextCount > 0)
        {
            _failNextCount--;
            throw new InvalidOperationException("Simulated MQTT publish failure.");
        }

        _messages.Enqueue(new PublishedMessage(topic, payloadJson, DateTimeOffset.UtcNow));
    }

    public sealed record PublishedMessage(string Topic, string PayloadJson, DateTimeOffset At);
}
