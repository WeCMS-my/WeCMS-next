using Microsoft.Extensions.Logging;

namespace WeCms.Modules.System.Security;

public sealed record SecurityAlertRecord(
    string EventType,
    string Severity,
    string Source,
    string Message,
    string TraceId,
    DateTimeOffset CreatedAt)
{
    public static SecurityAlertRecord FromSecurityEvent(
        string eventType,
        string severity,
        string message,
        string traceId,
        DateTimeOffset createdAt)
    {
        return new SecurityAlertRecord(eventType, severity, InferSource(eventType), message, traceId, createdAt);
    }

    private static string InferSource(string eventType)
    {
        if (eventType.Contains("2fa", StringComparison.OrdinalIgnoreCase)
            || eventType.Contains("two_factor", StringComparison.OrdinalIgnoreCase))
        {
            return "two-factor";
        }

        if (eventType.Contains("ban", StringComparison.OrdinalIgnoreCase))
        {
            return "security-ban";
        }

        if (eventType.Contains("ip_", StringComparison.OrdinalIgnoreCase))
        {
            return "ip-access";
        }

        if (eventType.Contains("rate_limit", StringComparison.OrdinalIgnoreCase))
        {
            return "rate-limit";
        }

        if (eventType.Contains("auth", StringComparison.OrdinalIgnoreCase)
            || eventType.Contains("login", StringComparison.OrdinalIgnoreCase)
            || eventType.Contains("refresh", StringComparison.OrdinalIgnoreCase))
        {
            return "auth";
        }

        return "security";
    }
}

public interface ISecurityAlertService
{
    Task PublishIfRequiredAsync(SecurityAlertRecord record, CancellationToken cancellationToken);
}

public interface ISecurityAlertSink
{
    Task SendAsync(SecurityAlertRecord record, CancellationToken cancellationToken);
}

public sealed class SecurityAlertService : ISecurityAlertService
{
    private static readonly IReadOnlySet<string> AlertSeverities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "high",
        "critical"
    };

    private readonly ISecurityAlertSink _sink;

    public SecurityAlertService(ISecurityAlertSink sink)
    {
        _sink = sink;
    }

    public Task PublishIfRequiredAsync(SecurityAlertRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        return AlertSeverities.Contains(record.Severity)
            ? _sink.SendAsync(record, cancellationToken)
            : Task.CompletedTask;
    }
}

public sealed class LoggingSecurityAlertSink : ISecurityAlertSink
{
    private readonly ILogger<LoggingSecurityAlertSink> _logger;

    public LoggingSecurityAlertSink(ILogger<LoggingSecurityAlertSink> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(SecurityAlertRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogCritical(
            "Security alert emitted. EventType: {EventType}; Severity: {Severity}; Source: {Source}; TraceId: {TraceId}; CreatedAt: {CreatedAt}; Message: {Message}",
            record.EventType,
            record.Severity,
            record.Source,
            record.TraceId,
            record.CreatedAt,
            record.Message);

        return Task.CompletedTask;
    }
}
