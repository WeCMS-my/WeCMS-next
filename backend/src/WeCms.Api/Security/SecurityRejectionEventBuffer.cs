using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeCms.Modules.Identity.Records;
using WeCms.Modules.Identity.Repositories;
using WeCms.Modules.Security;

namespace WeCms.Api.Security;

public interface ISecurityRejectionEventBuffer
{
    bool TryEnqueue(SecurityRejectionEvent record);
}

public sealed record IpAccessDeniedSecurityEvent(
    string Ip,
    string TraceId,
    DateTimeOffset CreatedAt,
    int RejectedCount = 1);

public sealed record SecurityRejectionEvent(
    SecurityRejectionEventKind Kind,
    RateLimitHitRecord? RateLimitHit,
    IpAccessDeniedSecurityEvent? IpAccessDenied)
{
    public static SecurityRejectionEvent FromRateLimit(RateLimitHitRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new SecurityRejectionEvent(SecurityRejectionEventKind.RateLimit, record, null);
    }

    public static SecurityRejectionEvent FromIpAccessDenied(IpAccessDeniedSecurityEvent record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new SecurityRejectionEvent(SecurityRejectionEventKind.IpAccessDenied, null, record);
    }
}

public enum SecurityRejectionEventKind
{
    RateLimit = 1,
    IpAccessDenied = 2
}

public sealed class SecurityRejectionEventBuffer : ISecurityRejectionEventBuffer
{
    private const int Capacity = 4096;
    private const int FlushBatchSize = 100;
    private static readonly TimeSpan AggregationWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DropLogInterval = TimeSpan.FromMinutes(1);

    private readonly object _gate = new();
    private readonly Dictionary<SecurityRejectionEventAggregateKey, SecurityRejectionEventAggregate> _aggregates = [];
    private readonly ILogger<SecurityRejectionEventBuffer> _logger;
    private readonly Dictionary<SecurityRejectionEventKind, long> _droppedByKind =
    new Dictionary<SecurityRejectionEventKind, long>
    {
        [SecurityRejectionEventKind.RateLimit] = 0,
        [SecurityRejectionEventKind.IpAccessDenied] = 0
    };

    private long _droppedCounter;
    private DateTimeOffset _lastDropAt;
    private DateTimeOffset _nextDropLogAt;

    public SecurityRejectionEventBuffer(ILogger<SecurityRejectionEventBuffer> logger)
    {
        _logger = logger;
    }

    public long SecurityRejectionEventBufferDroppedCounter => Interlocked.Read(ref _droppedCounter);

    public DateTimeOffset? LastDropAt => _lastDropAt == default ? null : _lastDropAt;

    public IReadOnlyDictionary<SecurityRejectionEventKind, long> DroppedByKind
    {
        get
        {
            lock (_gate)
            {
                return new Dictionary<SecurityRejectionEventKind, long>(_droppedByKind);
            }
        }
    }

    public ValueTask<SecurityRejectionEvent[]> DrainDueAsync(DateTimeOffset now, int maxItems)
    {
        if (maxItems <= 0)
        {
            return ValueTask.FromResult(Array.Empty<SecurityRejectionEvent>());
        }

        lock (_gate)
        {
            var keys = _aggregates
                .Where(pair => IsDue(pair.Key, now))
                .OrderBy(pair => pair.Key.WindowStart)
                .Take(maxItems)
                .Select(pair => pair.Key)
                .ToArray();

            if (keys.Length == 0)
            {
                return ValueTask.FromResult(Array.Empty<SecurityRejectionEvent>());
            }

            var dueEvents = new SecurityRejectionEvent[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                dueEvents[i] = _aggregates[keys[i]].ToSecurityRejectionEvent();
                _aggregates.Remove(keys[i]);
            }

            return ValueTask.FromResult(dueEvents);
        }
    }

    public bool TryEnqueue(SecurityRejectionEvent record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!TryCreateAggregate(record, out var key, out var aggregate))
        {
            return false;
        }

        lock (_gate)
        {
            if (_aggregates.TryGetValue(key, out var existing))
            {
                _aggregates[key] = existing.With(record);
                return true;
            }

            if (_aggregates.Count >= Capacity)
            {
                RegisterDropped(record.Kind);
                return false;
            }

            _aggregates.Add(key, aggregate);
            return true;
        }
    }

    private bool TryCreateAggregate(SecurityRejectionEvent record, out SecurityRejectionEventAggregateKey key, out SecurityRejectionEventAggregate aggregate)
    {
        if (record.Kind == SecurityRejectionEventKind.RateLimit && record.RateLimitHit is not null)
        {
            var rateLimitHit = record.RateLimitHit;
            if (rateLimitHit.Policy is not { Length: > 0 } policy
                || rateLimitHit.HttpMethod is not { Length: > 0 } method
                || rateLimitHit.Path is not { Length: > 0 } path
                || rateLimitHit.Ip is not { Length: > 0 } ip)
            {
                key = null!;
                aggregate = null!;
                return false;
            }

            var normalizedMethod = method.Trim().ToUpperInvariant();
            var normalizedPath = path.Trim();
            var normalizedIp = ip.Trim();
            var normalizedUserId = rateLimitHit.UserId;
            var normalizedUserAgent = rateLimitHit.UserAgent.Trim();
            var normalizedTraceId = rateLimitHit.TraceId.Trim();
            var windowStart = GetWindowStart(rateLimitHit.CreatedAt);

            key = new SecurityRejectionEventAggregateKey(
                SecurityRejectionEventKind.RateLimit,
                policy,
                normalizedMethod,
                normalizedPath,
                normalizedUserId,
                normalizedIp,
                windowStart);

            aggregate = new SecurityRejectionEventAggregate(
                SecurityRejectionEventKind.RateLimit,
                policy,
                normalizedMethod,
                normalizedPath,
                normalizedUserId,
                rateLimitHit.Username?.Trim(),
                normalizedIp,
                normalizedUserAgent,
                normalizedTraceId,
                rateLimitHit.CreatedAt,
                rateLimitHit.CreatedAt,
                1);

            return true;
        }

        if (record.Kind == SecurityRejectionEventKind.IpAccessDenied && record.IpAccessDenied is not null)
        {
            var ipAccessDenied = record.IpAccessDenied;
            if (string.IsNullOrWhiteSpace(ipAccessDenied.Ip) || string.IsNullOrWhiteSpace(ipAccessDenied.TraceId))
            {
                key = null!;
                aggregate = null!;
                return false;
            }

            if (ipAccessDenied.RejectedCount <= 0)
            {
                key = null!;
                aggregate = null!;
                return false;
            }

            var ip = ipAccessDenied.Ip.Trim();
            var traceId = ipAccessDenied.TraceId.Trim();
            var windowStart = GetWindowStart(ipAccessDenied.CreatedAt);

            key = new SecurityRejectionEventAggregateKey(
                SecurityRejectionEventKind.IpAccessDenied,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                ip,
                windowStart);

            aggregate = new SecurityRejectionEventAggregate(
                SecurityRejectionEventKind.IpAccessDenied,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                null,
                ip,
                string.Empty,
                traceId,
                ipAccessDenied.CreatedAt,
                ipAccessDenied.CreatedAt,
                ipAccessDenied.RejectedCount);

            return true;
        }

        key = null!;
        aggregate = null!;
        return false;
    }

    private void RegisterDropped(SecurityRejectionEventKind kind)
    {
        var now = DateTimeOffset.UtcNow;
        var dropped = Interlocked.Increment(ref _droppedCounter);
        bool shouldLog;
        long byKind;

        lock (_gate)
        {
            _droppedByKind[kind] = _droppedByKind[kind] + 1;
            _lastDropAt = now;
            shouldLog = now >= _nextDropLogAt;
            byKind = _droppedByKind[kind];
            if (shouldLog)
            {
                _nextDropLogAt = now + DropLogInterval;
            }
        }

        if (shouldLog)
        {
            _logger.LogWarning(
                "Security rejection event buffer is full. Dropped events={DroppedCounter}, droppedByKind={DroppedByKind}, lastDropAt={LastDropAt}.",
                dropped,
                byKind,
                now);
        }
    }

    private static DateTimeOffset GetWindowStart(DateTimeOffset value)
    {
        if (AggregationWindow == TimeSpan.Zero)
        {
            return value;
        }

        var ticks = value.UtcDateTime.Ticks / AggregationWindow.Ticks * AggregationWindow.Ticks;
        return new DateTimeOffset(new DateTime(ticks, DateTimeKind.Utc));
    }

    private static bool IsDue(SecurityRejectionEventAggregateKey key, DateTimeOffset now)
    {
        return key.WindowStart + AggregationWindow <= now;
    }

    private sealed record SecurityRejectionEventAggregate(
        SecurityRejectionEventKind Kind,
        string Policy,
        string HttpMethod,
        string Path,
        long? UserId,
        string? Username,
        string Ip,
        string UserAgent,
        string LastTraceId,
        DateTimeOffset FirstHitAt,
        DateTimeOffset LastHitAt,
        int HitCount)
    {
        public SecurityRejectionEventAggregate With(SecurityRejectionEvent record)
        {
            return record.Kind == SecurityRejectionEventKind.RateLimit && record.RateLimitHit is not null
                ? WithRateLimitHit(record.RateLimitHit)
                : WithIpAccessDenied(record.IpAccessDenied);
        }

        public SecurityRejectionEvent ToSecurityRejectionEvent()
        {
            return Kind switch
            {
                SecurityRejectionEventKind.RateLimit => SecurityRejectionEvent.FromRateLimit(new RateLimitHitRecord(
                    Policy,
                    HttpMethod,
                    Path,
                    UserId,
                    Username,
                    Ip,
                    UserAgent,
                    LastTraceId,
                    LastHitAt,
                    HitCount)),
                SecurityRejectionEventKind.IpAccessDenied => SecurityRejectionEvent.FromIpAccessDenied(new IpAccessDeniedSecurityEvent(
                    Ip,
                    LastTraceId,
                    LastHitAt,
                    HitCount)),
                _ => throw new InvalidOperationException($"Unsupported security rejection event kind: {Kind}.")
            };
        }

        private SecurityRejectionEventAggregate WithRateLimitHit(RateLimitHitRecord record)
        {
            return this with
            {
                LastTraceId = record.TraceId.Trim(),
                LastHitAt = record.CreatedAt,
                HitCount = HitCount + 1
            };
        }

        private SecurityRejectionEventAggregate WithIpAccessDenied(IpAccessDeniedSecurityEvent? record)
        {
            if (record is null)
            {
                return this;
            }

            return this with
            {
                LastTraceId = record.TraceId,
                LastHitAt = record.CreatedAt,
                HitCount = HitCount + record.RejectedCount
            };
        }
    }

    private sealed record SecurityRejectionEventAggregateKey(
        SecurityRejectionEventKind Kind,
        string Policy,
        string HttpMethod,
        string Path,
        long? UserId,
        string Ip,
        DateTimeOffset WindowStart);
}

public sealed class SecurityRejectionEventFlushHostedService : BackgroundService
{
    private const int MaxBatchSize = 100;
    private const string IpDeniedEventType = "security.ip_rejected";
    private const string IpDeniedMessage = "IP access is not allowed.";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecurityRejectionEventFlushHostedService> _logger;
    private readonly SecurityRejectionEventBuffer _reader;

    public SecurityRejectionEventFlushHostedService(
        SecurityRejectionEventBuffer reader,
        IServiceScopeFactory scopeFactory,
        ILogger<SecurityRejectionEventFlushHostedService> logger)
    {
        _reader = reader;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FlushByAggregateReaderAsync(stoppingToken);
        }
    }

    private async Task FlushByAggregateReaderAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var events = await _reader.DrainDueAsync(DateTimeOffset.UtcNow, MaxBatchSize);
            if (events.Length == 0)
            {
                return;
            }

            await FlushBatchAsync(events, cancellationToken);
        }
    }

    private async Task FlushBatchAsync(IReadOnlyList<SecurityRejectionEvent> events, CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            try
            {
                await FlushEventAsync(@event, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to flush buffered security rejection event.");
            }
        }
    }

    private async Task FlushEventAsync(SecurityRejectionEvent record, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        switch (record.Kind)
        {
            case SecurityRejectionEventKind.RateLimit:
                var rateLimitHit = record.RateLimitHit ?? throw new InvalidOperationException("Missing rate limit rejection record.");
                await scope.ServiceProvider.GetRequiredService<IRateLimitSecurityEventService>()
                    .RecordHitAsync(rateLimitHit, cancellationToken);
                break;
            case SecurityRejectionEventKind.IpAccessDenied:
                await FlushIpDeniedEventAsync(
                    scope.ServiceProvider,
                    record.IpAccessDenied ?? throw new InvalidOperationException("Missing IP access denied record."),
                    cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unsupported security rejection event kind: {record.Kind}.");
        }
    }

    private static async Task FlushIpDeniedEventAsync(
        IServiceProvider serviceProvider,
        IpAccessDeniedSecurityEvent record,
        CancellationToken cancellationToken)
    {
        var message = record.RejectedCount == 1
            ? IpDeniedMessage
            : $"{IpDeniedMessage} Rejected count: {record.RejectedCount}.";

        var securityEvent = new SecurityEventRecord(
            IpDeniedEventType,
            null,
            null,
            record.Ip,
            "critical",
            message,
            record.CreatedAt,
            record.TraceId);

        await serviceProvider.GetRequiredService<IAuthRepository>().RecordSecurityEventAsync(securityEvent, cancellationToken);
        await serviceProvider.GetRequiredService<ISecurityAlertService>().PublishIfRequiredAsync(
            SecurityAlertRecord.FromSecurityEvent(
                securityEvent.EventType,
                securityEvent.Severity,
                securityEvent.Message,
                securityEvent.TraceId,
                securityEvent.CreatedAt),
            cancellationToken);
    }
}
