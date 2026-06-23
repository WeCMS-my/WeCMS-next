using Microsoft.Extensions.DependencyInjection;
using WeCms.Modules.Security;

namespace WeCms.Api.RateLimiting;

public sealed class RateLimitSecurityEventFlushHostedService : BackgroundService
{
    private readonly IRateLimitHitBuffer _buffer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISecurityClock _clock;
    private readonly RateLimitSecurityEventFlushOptions _options;
    private readonly ILogger<RateLimitSecurityEventFlushHostedService> _logger;
    private int _consecutiveFailures;
    private DateTimeOffset? _circuitOpenUntil;

    public RateLimitSecurityEventFlushHostedService(
        IRateLimitHitBuffer buffer,
        IServiceScopeFactory scopeFactory,
        ISecurityClock clock,
        RateLimitSecurityEventFlushOptions options,
        ILogger<RateLimitSecurityEventFlushHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.FlushInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Rate limit flush interval must be positive.");
        }

        if (options.MaxBatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Rate limit flush max batch size must be positive.");
        }

        if (options.FailureThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Rate limit flush failure threshold must be positive.");
        }

        if (options.CircuitBreakerCooldown <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Rate limit flush circuit breaker cooldown must be positive.");
        }

        _buffer = buffer;
        _scopeFactory = scopeFactory;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    public bool CircuitBreakerOpen => IsCircuitOpen(_clock.UtcNow);

    public async Task FlushDueAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        if (IsCircuitOpen(now))
        {
            return;
        }

        var summaries = _buffer.DrainDue(now, _options.MaxBatchSize);
        foreach (var summary in summaries)
        {
            try
            {
                await RecordAsync(summary.ToHitRecord(), cancellationToken);
                _consecutiveFailures = 0;
                _circuitOpenUntil = null;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _consecutiveFailures++;
                _logger.LogWarning(
                    exception,
                    "Failed to flush aggregated rate-limit security event. ConsecutiveFailures={ConsecutiveFailures}",
                    _consecutiveFailures);

                if (_consecutiveFailures >= _options.FailureThreshold)
                {
                    _circuitOpenUntil = now.Add(_options.CircuitBreakerCooldown);
                    _logger.LogWarning(
                        "Rate-limit security event flush circuit breaker opened until {CircuitOpenUntil}.",
                        _circuitOpenUntil);
                }

                return;
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.FlushInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FlushDueAsync(stoppingToken);
        }
    }

    private bool IsCircuitOpen(DateTimeOffset now)
    {
        return _circuitOpenUntil is not null && _circuitOpenUntil > now;
    }

    private async Task RecordAsync(RateLimitHitRecord record, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var recorder = scope.ServiceProvider.GetRequiredService<IRateLimitSecurityEventService>();
        await recorder.RecordHitAsync(record, cancellationToken);
    }
}
