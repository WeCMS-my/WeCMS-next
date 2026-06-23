using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Modules.Security;

public sealed record RateLimitHitRecord(
    string Policy,
    string HttpMethod,
    string Path,
    long? UserId,
    string? Username,
    string Ip,
    string UserAgent,
    string TraceId,
    DateTimeOffset CreatedAt,
    int RejectedCount = 1);

public sealed record RateLimitSecurityEventRecord(
    string EventType,
    string Policy,
    string HttpMethod,
    string Path,
    long? UserId,
    string? Username,
    string Ip,
    string Severity,
    string Source,
    string Message,
    string TraceId,
    DateTimeOffset CreatedAt);

public interface IRateLimitSecurityEventService
{
    Task RecordHitAsync(RateLimitHitRecord record, CancellationToken cancellationToken);
}

public interface IRateLimitSecurityEventRepository
{
    Task RecordHitAsync(RateLimitSecurityEventRecord record, CancellationToken cancellationToken);
}

public sealed class RateLimitSecurityEventService : IRateLimitSecurityEventService
{
    private readonly IRateLimitSecurityEventRepository _repository;
    private readonly ISecurityAlertService _alertService;

    public RateLimitSecurityEventService(IRateLimitSecurityEventRepository repository, ISecurityAlertService alertService)
    {
        _repository = repository;
        _alertService = alertService;
    }

    public async Task RecordHitAsync(RateLimitHitRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!RateLimitPolicyNames.All.Contains(record.Policy))
        {
            throw new InvalidOperationException($"Unknown rate limit policy: {record.Policy}.");
        }

        var path = NormalizeRequired(record.Path, "path", 256);
        var method = NormalizeRequired(record.HttpMethod, "httpMethod", 16).ToUpperInvariant();
        var ip = NormalizeRequired(record.Ip, "ip", 64);
        var traceId = NormalizeRequired(record.TraceId, "traceId", 64);
        if (record.RejectedCount <= 0)
        {
            throw new InvalidOperationException("rejectedCount must be positive for rate limit security events.");
        }

        var message = record.RejectedCount == 1
            ? $"Rate limit hit for {record.Policy} on {method} {path}."
            : $"Rate limit hit for {record.Policy} on {method} {path}. Rejected count: {record.RejectedCount}.";

        var securityEvent = new RateLimitSecurityEventRecord(
            "rate_limit_hit",
            record.Policy,
            method,
            path,
            record.UserId,
            NormalizeOptional(record.Username, 64),
            ip,
            "warning",
            "rate-limit",
            message,
            traceId,
            record.CreatedAt);

        await _repository.RecordHitAsync(securityEvent, cancellationToken);
        await _alertService.PublishIfRequiredAsync(
            new SecurityAlertRecord(
                securityEvent.EventType,
                securityEvent.Severity,
                securityEvent.Source,
                securityEvent.Message,
                securityEvent.TraceId,
                securityEvent.CreatedAt),
            cancellationToken);
    }

    private static string NormalizeRequired(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{field} is required for rate limit security events.");
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : throw new DomainException(ApiCodes.ValidationError, $"{field} must be {maxLength} characters or fewer.");
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : throw new DomainException(ApiCodes.ValidationError, $"value must be {maxLength} characters or fewer.");
    }
}
