using System.Net.Sockets;

namespace API.IntegrationTests.Support;

/// <summary>
/// Gates live Mosquitto / MQTT broker tests. Never invents success when the broker is down.
/// Configure via ESP32_SECURITY_TEST_MQTT_HOST / ESP32_SECURITY_TEST_MQTT_PORT (default localhost:1883).
/// </summary>
internal static class MqttBrokerTestGate
{
    public const string HostEnv = "ESP32_SECURITY_TEST_MQTT_HOST";
    public const string PortEnv = "ESP32_SECURITY_TEST_MQTT_PORT";

    public static string Host =>
        Environment.GetEnvironmentVariable(HostEnv)?.Trim() is { Length: > 0 } host
            ? host
            : "127.0.0.1";

    public static int Port
    {
        get
        {
            var raw = Environment.GetEnvironmentVariable(PortEnv);
            return int.TryParse(raw, out var port) && port is > 0 and < 65536 ? port : 1883;
        }
    }

    public static string SkipReason =>
        $"Live MQTT broker not reachable at {Host}:{Port}. " +
        $"Start an isolated Mosquitto (or Testcontainers) and set {HostEnv}/{PortEnv}. " +
        "Fake MQTT / RecordingMqttOutboundPublisher is not evidence of live broker integration.";

    public static async Task<(bool Available, string Detail)> ProbeAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(TimeSpan.FromSeconds(2));
            await client.ConnectAsync(Host, Port, linked.Token);
            return (true, $"TCP connect ok {Host}:{Port}");
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or IOException)
        {
            return (false, $"{SkipReason} ({ex.GetType().Name}: {ex.Message})");
        }
    }
}
