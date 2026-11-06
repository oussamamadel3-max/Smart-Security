using Application.Interfaces.Services;
using Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Optional outbox poller. Remains idle unless <see cref="MqttOutboxOptions.Enabled"/> is true.
/// Live broker publish still requires a publisher with <c>IsLivePublishEnabled</c> (G1/ops).
/// </summary>
public sealed class MqttOutboxHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptionsMonitor<MqttOutboxOptions> _options;
    private readonly ILogger<MqttOutboxHostedService> _logger;

    public MqttOutboxHostedService(
        IServiceScopeFactory scopes,
        IOptionsMonitor<MqttOutboxOptions> options,
        ILogger<MqttOutboxHostedService> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.CurrentValue;
        if (!opts.Enabled)
        {
            _logger.LogInformation(
                "MQTT outbox publisher is disabled (Mqtt:Outbox:Enabled=false). " +
                "Command rows still persist with outbox messages; dispatch is available via IOutboxDispatchService for tests.");
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }

            return;
        }

        _logger.LogWarning(
            "Mqtt:Outbox:Enabled=true. Worker will poll durable outbox rows. " +
            "Live Mosquitto publish still depends on IMqttOutboundPublisher.IsLivePublishEnabled (G1/ops).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
                await dispatch.MarkTimedOutCommandsAsync(stoppingToken);
                await dispatch.DispatchPendingCommandsAsync(stoppingToken);
                await dispatch.DispatchPendingEventPersistedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatch cycle failed.");
            }

            var delay = Math.Clamp(_options.CurrentValue.PollIntervalMs, 200, 60_000);
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
