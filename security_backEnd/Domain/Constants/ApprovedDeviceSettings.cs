namespace Domain.Constants;

/// <summary>
/// Product-approved device setting timers from D-03 decisions that are already fixed.
/// Event buffer capacity and command TTL remain explicitly supplied per registration
/// until measured production defaults are adopted.
/// </summary>
public static class ApprovedDeviceSettings
{
    public const int TestDurationMs = 30_000;
    public const int HeartbeatTimeoutSeconds = 3600;
    public const int CommandTimeoutSeconds = 10;
    public const long InitialSettingsVersion = 1;
}
