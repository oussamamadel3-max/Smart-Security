using Application.Interfaces.Services;

namespace API.IntegrationTests.Support;

/// <summary>
/// Test double that records broker revoke calls and can be forced to fail.
/// </summary>
public sealed class RecordingDeviceBrokerAccessService : IDeviceBrokerAccessService
{
    private readonly List<(Guid DeviceId, string HardwareUid)> _calls = new();
    private readonly object _gate = new();

    public bool FailNext { get; set; }

    public IReadOnlyList<(Guid DeviceId, string HardwareUid)> Calls
    {
        get
        {
            lock (_gate)
                return _calls.ToList();
        }
    }

    public Task RevokeAccessAsync(Guid deviceId, string hardwareUid, CancellationToken ct = default)
    {
        lock (_gate)
            _calls.Add((deviceId, hardwareUid));

        if (FailNext)
            throw new InvalidOperationException("Simulated broker revoke failure.");

        return Task.CompletedTask;
    }
}
