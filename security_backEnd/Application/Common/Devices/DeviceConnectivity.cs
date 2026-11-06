using Domain.Constants;

namespace Application.Common.Devices;

/// <summary>
/// Computes ONLINE/OFFLINE from last_seen_at and heartbeat timeout — never persisted as is_online.
/// </summary>
public static class DeviceConnectivity
{
    public static string ComputeStatus(
        DateTimeOffset? lastSeenAt,
        int heartbeatTimeoutSeconds,
        DateTimeOffset utcNow)
    {
        if (heartbeatTimeoutSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(heartbeatTimeoutSeconds));

        if (lastSeenAt is null)
            return MqttInboundDraft.ConnectionStatus.Offline;

        var age = utcNow - lastSeenAt.Value;
        return age <= TimeSpan.FromSeconds(heartbeatTimeoutSeconds)
            ? MqttInboundDraft.ConnectionStatus.Online
            : MqttInboundDraft.ConnectionStatus.Offline;
    }

    public static bool IsOnline(
        DateTimeOffset? lastSeenAt,
        int heartbeatTimeoutSeconds,
        DateTimeOffset utcNow)
        => ComputeStatus(lastSeenAt, heartbeatTimeoutSeconds, utcNow)
           == MqttInboundDraft.ConnectionStatus.Online;
}
