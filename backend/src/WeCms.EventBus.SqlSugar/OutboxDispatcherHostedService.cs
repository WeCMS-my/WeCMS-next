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
        var idlePollInterval = ValidatedIdlePollInterval();
        var failurePollInterval = ValidatedFailurePollInterval();

        while (!stoppingToken.IsCancellationRequested)
        {
            var outcome = await DispatchOnceAsync(stoppingToken).ConfigureAwait(false);
            var delay = SelectDelay(outcome, pollInterval, idlePollInterval, failurePollInterval);

            await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task<OutboxDispatchCycleOutcome> DispatchOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
            OutboxDispatchResult result = await dispatcher.DispatchAsync(stoppingToken).ConfigureAwait(false);

            return result.FailedCount > 0
                ? OutboxDispatchCycleOutcome.Failed
                : result.LockedCount == 0
                ? OutboxDispatchCycleOutcome.Idle
                : OutboxDispatchCycleOutcome.Processed;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Outbox dispatcher hosted service iteration failed.");
            return OutboxDispatchCycleOutcome.Failed;
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

    private TimeSpan ValidatedIdlePollInterval()
    {
        if (options.IdlePollInterval < options.PollInterval)
        {
            throw new InvalidOperationException("Outbox dispatcher idle poll interval must be greater than or equal to the poll interval.");
        }

        return options.IdlePollInterval;
    }

    private TimeSpan ValidatedFailurePollInterval()
    {
        if (options.FailurePollInterval < options.PollInterval)
        {
            throw new InvalidOperationException("Outbox dispatcher failure poll interval must be greater than or equal to the poll interval.");
        }

        return options.FailurePollInterval;
    }

    private static TimeSpan SelectDelay(
        OutboxDispatchCycleOutcome outcome,
        TimeSpan pollInterval,
        TimeSpan idlePollInterval,
        TimeSpan failurePollInterval)
    {
        return outcome switch
        {
            OutboxDispatchCycleOutcome.Idle => idlePollInterval,
            OutboxDispatchCycleOutcome.Failed => failurePollInterval,
            _ => pollInterval
        };
    }

    private enum OutboxDispatchCycleOutcome
    {
        Processed,
        Idle,
        Failed
    }
}
