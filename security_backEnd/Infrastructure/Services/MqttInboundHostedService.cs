using Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Optional broker subscription host. Remains idle unless <see cref="MqttInboundOptions.Enabled"/> is true.
/// Real Mosquitto wiring stays behind G1/ops; Application ingest is exercised via dispatcher/tests.
/// </summary>
public sealed class MqttInboundHostedService : BackgroundService
{
    private readonly IOptionsMonitor<MqttInboundOptions> _options;
    private readonly ILogger<MqttInboundHostedService> _logger;

    public MqttInboundHostedService(
        IOptionsMonitor<MqttInboundOptions> options,
        ILogger<MqttInboundHostedService> logger)
    {
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.CurrentValue;
        if (!opts.Enabled)
        {
            _logger.LogInformation(
                "MQTT inbound consumer is disabled (Mqtt:Inbound:Enabled=false). " +
                "Device telemetry ingest remains available through IDeviceInboundTelemetryService.");
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }

            return;
        }

        // Broker client integration is intentionally not hard-coded here before G1/ops secrets.
        _logger.LogWarning(
            "Mqtt:Inbound:Enabled=true but no production Mosquitto adapter is registered yet. " +
            "Configure ops broker wiring before relying on live subscriptions. Host={Host} Port={Port}",
            opts.Host,
            opts.Port);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
