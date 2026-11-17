namespace Domain.Constants;

/// <summary>
/// Initial development and test timers. They are not a measured production capacity or command TTL.
/// Event buffer capacity and command TTL remain explicitly supplied per registration.
/// </summary>
public static class ApprovedDeviceSettings
{
    public const int TestDurationMs = 30_000;
    public const int HeartbeatTimeoutSeconds = 3600;
    public const int CommandTimeoutSeconds = 10;
    public const long InitialSettingsVersion = 1;
}
