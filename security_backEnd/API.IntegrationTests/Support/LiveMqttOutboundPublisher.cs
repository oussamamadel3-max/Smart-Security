using Application.Interfaces.Services;
using MQTTnet;
using MQTTnet.Protocol;

namespace API.IntegrationTests.Support;

/// <summary>
/// Test-only outbound publisher that talks to a real broker when MqttBrokerTestGate succeeds.
/// Not registered in production DI; G1/ops still own the production Mosquitto adapter.
/// </summary>
internal sealed class LiveMqttOutboundPublisher : IMqttOutboundPublisher, IAsyncDisposable
{
    private readonly IMqttClient _client;
    private readonly string _host;
    private readonly int _port;

    public LiveMqttOutboundPublisher(string host, int port)
    {
        _host = host;
        _port = port;
        _client = new MqttClientFactory().CreateMqttClient();
    }

    public bool IsLivePublishEnabled => true;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_client.IsConnected)
            return;

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .WithClientId($"be-t10-out-{Guid.NewGuid():N}")
            .WithCleanSession()
            .Build();

        var result = await _client.ConnectAsync(options, ct);
        if (result.ResultCode != MqttClientConnectResultCode.Success)
            throw new InvalidOperationException($"MQTT connect failed: {result.ResultCode}");
    }

    public async Task PublishAsync(string topic, string payloadJson, CancellationToken ct = default)
    {
        await ConnectAsync(ct);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payloadJson)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build();

        var result = await _client.PublishAsync(message, ct);
        if (result.ReasonCode != MqttClientPublishReasonCode.Success
            && result.ReasonCode != MqttClientPublishReasonCode.NoMatchingSubscribers)
        {
            throw new InvalidOperationException($"MQTT publish failed: {result.ReasonCode}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync();
        _client.Dispose();
    }
}
