namespace Application.Interfaces.Services;

/// <summary>
/// Revokes MQTT broker authentication/ACL and terminates any active device session (FR-007 / FR-034).
/// Production Mosquitto adapter is expected later; archive must not succeed unless this completes.
/// </summary>
public interface IDeviceBrokerAccessService
{
    Task RevokeAccessAsync(Guid deviceId, string hardwareUid, CancellationToken ct = default);
}
