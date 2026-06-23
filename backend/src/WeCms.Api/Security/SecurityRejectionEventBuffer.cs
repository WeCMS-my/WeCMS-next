using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using WeCms.Modules.Identity.Records;
using WeCms.Modules.Identity.Repositories;
using WeCms.Modules.Security;

namespace WeCms.Api.Security;

public interface ISecurityRejectionEventBuffer
{
    bool TryEnqueue(SecurityRejectionEvent record);
}

public interface ISecurityRejectionEventReader
{
    IAsyncEnumerable<SecurityRejectionEvent> ReadAllAsync(CancellationToken cancellationToken);

    bool TryRead(out SecurityRejectionEvent record);
}

public sealed record IpAccessDeniedSecurityEvent(
    string Ip,
    string TraceId,
    DateTimeOffset CreatedAt);

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

public sealed class SecurityRejectionEventBuffer : ISecurityRejectionEventBuffer, ISecurityRejectionEventReader
{
    private const int Capacity = 4096;

    private readonly Channel<SecurityRejectionEvent> _channel = Channel.CreateBounded<SecurityRejectionEvent>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public bool TryEnqueue(SecurityRejectionEvent record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return _channel.Writer.TryWrite(record);
    }

    public IAsyncEnumerable<SecurityRejectionEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public bool TryRead(out SecurityRejectionEvent record)
    {
        return _channel.Reader.TryRead(out record!);
    }
}

public sealed class SecurityRejectionEventFlushHostedService : BackgroundService
{
    private const int MaxBatchSize = 100;
    private const string IpDeniedEventType = "security.ip_rejected";
    private const string IpDeniedMessage = "IP access is not allowed.";

    private readonly ISecurityRejectionEventReader _reader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecurityRejectionEventFlushHostedService> _logger;

    public SecurityRejectionEventFlushHostedService(
        ISecurityRejectionEventReader reader,
        IServiceScopeFactory scopeFactory,
        ILogger<SecurityRejectionEventFlushHostedService> logger)
    {
        _reader = reader;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var first in _reader.ReadAllAsync(stoppingToken))
        {
            var batch = new List<SecurityRejectionEvent>(MaxBatchSize) { first };
            while (batch.Count < MaxBatchSize && _reader.TryRead(out var next))
            {
                batch.Add(next);
            }

            await FlushBatchAsync(batch, stoppingToken);
        }
    }

    private async Task FlushBatchAsync(IReadOnlyList<SecurityRejectionEvent> events, CancellationToken cancellationToken)
    {
        foreach (var group in events.GroupBy(SecurityRejectionEventKey.From))
        {
            try
            {
                await FlushEventAsync(group.First(), group.Count(), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to flush buffered security rejection event.");
            }
        }
    }

    private async Task FlushEventAsync(SecurityRejectionEvent record, int count, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        switch (record.Kind)
        {
            case SecurityRejectionEventKind.RateLimit:
                await scope.ServiceProvider.GetRequiredService<IRateLimitSecurityEventService>()
                    .RecordHitAsync(WithRejectedCount(record.RateLimitHit ?? throw new InvalidOperationException("Missing rate limit rejection record."), count), cancellationToken);
                break;
            case SecurityRejectionEventKind.IpAccessDenied:
                await FlushIpDeniedEventAsync(scope.ServiceProvider, record.IpAccessDenied ?? throw new InvalidOperationException("Missing IP access denied record."), count, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unsupported security rejection event kind: {record.Kind}.");
        }
    }

    private static RateLimitHitRecord WithRejectedCount(RateLimitHitRecord record, int count)
    {
        return record with { RejectedCount = count };
    }

    private static async Task FlushIpDeniedEventAsync(
        IServiceProvider serviceProvider,
        IpAccessDeniedSecurityEvent record,
        int count,
        CancellationToken cancellationToken)
    {
        var message = count == 1 ? IpDeniedMessage : $"{IpDeniedMessage} Rejected count: {count}.";
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

    private sealed record SecurityRejectionEventKey(
        SecurityRejectionEventKind Kind,
        string Policy,
        string Method,
        string Path,
        long? UserId,
        string Username,
        string Ip)
    {
        public static SecurityRejectionEventKey From(SecurityRejectionEvent record)
        {
            return record.Kind switch
            {
                SecurityRejectionEventKind.RateLimit => FromRateLimit(record.RateLimitHit ?? throw new InvalidOperationException("Missing rate limit rejection record.")),
                SecurityRejectionEventKind.IpAccessDenied => FromIpAccessDenied(record.IpAccessDenied ?? throw new InvalidOperationException("Missing IP access denied record.")),
                _ => throw new InvalidOperationException($"Unsupported security rejection event kind: {record.Kind}.")
            };
        }

        private static SecurityRejectionEventKey FromRateLimit(RateLimitHitRecord record)
        {
            return new SecurityRejectionEventKey(
                SecurityRejectionEventKind.RateLimit,
                record.Policy,
                record.HttpMethod,
                record.Path,
                record.UserId,
                record.Username ?? string.Empty,
                record.Ip);
        }

        private static SecurityRejectionEventKey FromIpAccessDenied(IpAccessDeniedSecurityEvent record)
        {
            return new SecurityRejectionEventKey(
                SecurityRejectionEventKind.IpAccessDenied,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                string.Empty,
                record.Ip);
        }
    }
}
