namespace WeCms.Modules.Security;

public sealed record RateLimitHitBufferOptions(TimeSpan Window, int MaxAggregateKeys)
{
    public static RateLimitHitBufferOptions Default { get; } = new(TimeSpan.FromMinutes(1), 4096);
}

public sealed record RateLimitSecurityEventFlushOptions(
    TimeSpan FlushInterval,
    int MaxBatchSize,
    int FailureThreshold,
    TimeSpan CircuitBreakerCooldown)
{
    public static RateLimitSecurityEventFlushOptions Default { get; } = new(
        TimeSpan.FromSeconds(10),
        256,
        3,
        TimeSpan.FromMinutes(1));
}

public sealed record RateLimitHitSummary(
    string Policy,
    string HttpMethod,
    string Path,
    long? UserId,
    string? Username,
    string Ip,
    string UserAgent,
    string TraceId,
    DateTimeOffset FirstHitAt,
    DateTimeOffset LastHitAt,
    int HitCount)
{
    public RateLimitHitRecord ToHitRecord()
    {
        return new RateLimitHitRecord(
            Policy,
            HttpMethod,
            Path,
            UserId,
            Username,
            Ip,
            UserAgent,
            TraceId,
            LastHitAt,
            HitCount);
    }
}

public interface IRateLimitHitBuffer
{
    bool TryRecord(RateLimitHitRecord record);

    IReadOnlyList<RateLimitHitSummary> DrainDue(DateTimeOffset now, int maxItems = int.MaxValue);
}

public interface IRateLimitHitAggregator
{
    bool TryAggregate(RateLimitHitRecord record);

    IReadOnlyList<RateLimitHitSummary> DrainDue(DateTimeOffset now, int maxItems = int.MaxValue);
}

public sealed class InMemoryRateLimitHitBuffer : IRateLimitHitBuffer, IRateLimitHitAggregator
{
    private readonly object _gate = new();
    private readonly Dictionary<RateLimitHitAggregateKey, RateLimitHitAggregate> _aggregates = [];
    private readonly RateLimitHitBufferOptions _options;

    public InMemoryRateLimitHitBuffer(RateLimitHitBufferOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Window < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Rate limit aggregation window must not be negative.");
        }

        if (options.MaxAggregateKeys <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Rate limit max aggregate keys must be positive.");
        }

        _options = options;
    }

    public bool TryRecord(RateLimitHitRecord record)
    {
        return TryAggregate(record);
    }

    public bool TryAggregate(RateLimitHitRecord record)
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
                _aggregates[key] = existing.Add(record);
                return true;
            }

            if (_aggregates.Count >= _options.MaxAggregateKeys)
            {
                return false;
            }

            _aggregates.Add(key, aggregate);
            return true;
        }
    }

    public IReadOnlyList<RateLimitHitSummary> DrainDue(DateTimeOffset now, int maxItems = int.MaxValue)
    {
        if (maxItems <= 0)
        {
            return [];
        }

        lock (_gate)
        {
            var dueKeys = _aggregates
                .Where(pair => pair.Key.WindowStartedAt + _options.Window <= now)
                .OrderBy(pair => pair.Key.WindowStartedAt)
                .Take(maxItems)
                .Select(pair => pair.Key)
                .ToArray();

            if (dueKeys.Length == 0)
            {
                return [];
            }

            var summaries = new List<RateLimitHitSummary>(dueKeys.Length);
            foreach (var key in dueKeys)
            {
                summaries.Add(_aggregates[key].ToSummary());
                _aggregates.Remove(key);
            }

            return summaries;
        }
    }

    private bool TryCreateAggregate(
        RateLimitHitRecord record,
        out RateLimitHitAggregateKey key,
        out RateLimitHitAggregate aggregate)
    {
        key = default;
        aggregate = default;

        var policy = Normalize(record.Policy, 128);
        var method = Normalize(record.HttpMethod, 16)?.ToUpperInvariant();
        var path = Normalize(record.Path, 256);
        var ip = Normalize(record.Ip, 64);
        var traceId = Normalize(record.TraceId, 64);
        if (policy is null || method is null || path is null || ip is null || traceId is null)
        {
            return false;
        }

        var windowStartedAt = GetWindowStart(record.CreatedAt);
        key = new RateLimitHitAggregateKey(policy, method, path, record.UserId, ip, windowStartedAt);
        aggregate = RateLimitHitAggregate.Create(
            policy,
            method,
            path,
            record.UserId,
            Normalize(record.Username, 64),
            ip,
            Normalize(record.UserAgent, 256) ?? string.Empty,
            traceId,
            record.CreatedAt);
        return true;
    }

    private DateTimeOffset GetWindowStart(DateTimeOffset value)
    {
        if (_options.Window == TimeSpan.Zero)
        {
            return value;
        }

        var ticks = value.UtcDateTime.Ticks / _options.Window.Ticks * _options.Window.Ticks;
        return new DateTimeOffset(new DateTime(ticks, DateTimeKind.Utc));
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private readonly record struct RateLimitHitAggregateKey(
        string Policy,
        string HttpMethod,
        string Path,
        long? UserId,
        string Ip,
        DateTimeOffset WindowStartedAt);

    private readonly record struct RateLimitHitAggregate(
        string Policy,
        string HttpMethod,
        string Path,
        long? UserId,
        string? Username,
        string Ip,
        string UserAgent,
        string TraceId,
        DateTimeOffset FirstHitAt,
        DateTimeOffset LastHitAt,
        int HitCount)
    {
        public static RateLimitHitAggregate Create(
            string policy,
            string httpMethod,
            string path,
            long? userId,
            string? username,
            string ip,
            string userAgent,
            string traceId,
            DateTimeOffset createdAt)
        {
            return new RateLimitHitAggregate(
                policy,
                httpMethod,
                path,
                userId,
                username,
                ip,
                userAgent,
                traceId,
                createdAt,
                createdAt,
                1);
        }

        public RateLimitHitAggregate Add(RateLimitHitRecord record)
        {
            return this with
            {
                UserAgent = Normalize(record.UserAgent, 256) ?? UserAgent,
                TraceId = Normalize(record.TraceId, 64) ?? TraceId,
                LastHitAt = record.CreatedAt,
                HitCount = HitCount + 1
            };
        }

        public RateLimitHitSummary ToSummary()
        {
            return new RateLimitHitSummary(
                Policy,
                HttpMethod,
                Path,
                UserId,
                Username,
                Ip,
                UserAgent,
                TraceId,
                FirstHitAt,
                LastHitAt,
                HitCount);
        }
    }
}
