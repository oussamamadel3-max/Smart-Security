using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Development placeholder for MQTT broker ACL/session revoke.
/// Records intent via logging only; replace with a real Mosquitto/ops adapter before AC-T-19 / G2.
/// </summary>
public sealed class NoOpDeviceBrokerAccessService : IDeviceBrokerAccessService
{
    private readonly ILogger<NoOpDeviceBrokerAccessService> _logger;

    public NoOpDeviceBrokerAccessService(ILogger<NoOpDeviceBrokerAccessService> logger)
    {
        _logger = logger;
    }

    public Task RevokeAccessAsync(Guid deviceId, string hardwareUid, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Device broker access revoke accepted locally (no-op adapter). DeviceId={DeviceId} HardwareUid={HardwareUid}",
            deviceId,
            hardwareUid);
        return Task.CompletedTask;
    }
}
