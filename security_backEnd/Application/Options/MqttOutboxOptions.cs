namespace Application.Options;

/// <summary>
/// Outbox → MQTT publish worker options.
/// Disabled by default until G1 adopts the command contract and ops wires a trusted broker.
/// </summary>
public sealed class MqttOutboxOptions
{
    public const string SectionName = "Mqtt:Outbox";

    /// <summary>
    /// When false, the hosted publisher idles; Application dispatch remains callable for tests with a recording publisher.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Poll interval for the hosted worker when enabled.
    /// </summary>
    public int PollIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Max outbox rows claimed per dispatch cycle.
    /// </summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// Delay before retrying a failed publish attempt.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Soft ceiling on publish attempts before parking the row with last_error_code (does not delete the command).
    /// </summary>
    public int MaxAttempts { get; set; } = 10;
}
