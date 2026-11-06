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
}
