using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WeCms.EventBus;

public sealed class OutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly OutboxDispatcherOptions options;
    private readonly ILogger<OutboxDispatcherHostedService> logger;

    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        OutboxDispatcherOptions options,
        ILogger<OutboxDispatcherHostedService> logger)
    {
        this.scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = ValidatedPollInterval();

        while (!stoppingToken.IsCancellationRequested)
        {
            await DispatchOnceAsync(stoppingToken).ConfigureAwait(false);
            await Task.Delay(pollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
            await dispatcher.DispatchAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Outbox dispatcher hosted service iteration failed.");
        }
    }

    private TimeSpan ValidatedPollInterval()
    {
        if (options.PollInterval < TimeSpan.FromMilliseconds(100))
        {
            throw new InvalidOperationException("Outbox dispatcher poll interval must be at least 100 milliseconds.");
        }

        return options.PollInterval;
    }
}
