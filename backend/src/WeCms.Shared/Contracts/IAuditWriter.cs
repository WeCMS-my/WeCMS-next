namespace WeCms.Shared.Contracts;

public interface IAuditWriter
{
    Task LogAsync(string module, string action, long? userId, string? username, string? ip, string? userAgent, int? statusCode, string result, CancellationToken ct);
    Task LogAsync(string module, string action, long? userId, string? username, string? ip, string? userAgent, int? statusCode, string result, string? errorMessage, CancellationToken ct);
}
