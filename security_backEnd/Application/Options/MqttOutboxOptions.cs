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

/// <summary>
/// Live broker publish for the outbox. Stays off unless an environment explicitly opts in.
/// Passwords are read from configuration or environment variables and must not be committed.
/// </summary>
public sealed class MqttLivePublishOptions
{
    public const string SectionName = "Mqtt:Outbound";

    /// <summary>
    /// When false, DI registers <c>NoOpMqttOutboundPublisher</c> and no broker connection is opened.
    /// </summary>
    public bool LivePublishEnabled { get; set; }

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1883;

    public string ClientId { get; set; } = "smart-security-backend-outbound";

    /// <summary>Server MQTT username. Never commit a secret.</summary>
    public string? Username { get; set; }

    /// <summary>Server MQTT password. Never commit a secret.</summary>
    public string? Password { get; set; }

    public int KeepAliveSeconds { get; set; } = 15;

    /// <summary>
    /// When true, publish uses TLS and validates the server certificate against <see cref="CaCertificatePath"/>.
    /// Stays false unless an environment opts in.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// PEM file of the trusted CA. Required when <see cref="UseTls"/> is true. Never a private key.
    /// </summary>
    public string? CaCertificatePath { get; set; }
}
