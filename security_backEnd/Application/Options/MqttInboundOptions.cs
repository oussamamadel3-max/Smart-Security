namespace Application.Options;

/// <summary>
/// Optional MQTT broker subscription for inbound device telemetry.
/// Disabled by default until ops/G1 provide a trusted broker endpoint.
/// </summary>
public sealed class MqttInboundOptions
{
    public const string SectionName = "Mqtt:Inbound";

    /// <summary>
    /// When false, no broker connection is attempted; Application ingest remains available for tests.
    /// </summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1883;

    public string ClientId { get; set; } = "smart-security-backend-inbound";

    /// <summary>
    /// Server-side MQTT username; never commit secrets — use User Secrets / environment.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Server-side MQTT password; never commit secrets — use User Secrets / environment.
    /// </summary>
    public string? Password { get; set; }

    public string StateTopicFilter { get; set; } = "devices/+/state";

    public string AcksTopicFilter { get; set; } = "devices/+/acks";

    /// <summary>
    /// ALARM_EVENT, EVENT_LOSS_REPORT, and DEVICE_FAULT share the existing events channel.
    /// </summary>
    public string EventsTopicFilter { get; set; } = "devices/+/events";

    /// <summary>
    /// MQTT keepalive. Short values surface a dead broker sooner; they are not a command TTL.
    /// </summary>
    public int KeepAliveSeconds { get; set; } = 15;

    /// <summary>
    /// MQTT 5 session expiry advertised at connect and at a graceful disconnect.
    /// Zero would drop queued QoS 1 messages when the process stops.
    /// </summary>
    public int SessionExpirySeconds { get; set; } = 3600;

    /// <summary>
    /// When true, the broker connection uses TLS and validates the server certificate against <see cref="CaCertificatePath"/>.
    /// Stays false unless an environment opts in. Do not commit private keys.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// PEM file of the trusted CA. Required when <see cref="UseTls"/> is true. Never a private key.
    /// </summary>
    public string? CaCertificatePath { get; set; }
}
