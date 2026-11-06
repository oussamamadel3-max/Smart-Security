using Application.Interfaces.Services;
using Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Optional FCM poller. Remains idle unless <see cref="FcmOptions.Enabled"/> is true.
/// Real Firebase traffic still requires <c>Fcm:Provider=Firebase</c> plus ADC credentials.
/// </summary>
public sealed class FcmNotificationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptionsMonitor<FcmOptions> _options;
    private readonly ILogger<FcmNotificationHostedService> _logger;

    public FcmNotificationHostedService(
        IServiceScopeFactory scopes,
        IOptionsMonitor<FcmOptions> options,
        ILogger<FcmNotificationHostedService> logger)
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
                "FCM notification worker is disabled (Fcm:Enabled=false). " +
                "Dispatch remains available via INotificationDispatchService for tests. Provider={Provider}.",
                opts.Provider);
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
            "Fcm:Enabled=true. Worker will poll notification queue using provider {Provider}. " +
            "ACCEPTED means provider acceptance only — not device receipt or user-read.",
            opts.Provider);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var dispatch = scope.ServiceProvider.GetRequiredService<INotificationDispatchService>();
                await dispatch.DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM dispatch cycle failed.");
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
