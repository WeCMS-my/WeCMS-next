namespace WeCms.Modules.Identity.Services;

public static class IdentitySecurityBanTypes
{
    public const string Ip = "ip";
    public const string User = "user";
}

public sealed record IdentitySecurityBanCreateRecord(
    string BanType,
    string Target,
    string Reason,
    string Severity,
    string Source,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);

public interface IIdentitySecurityAlertService
{
    Task PublishIfRequiredAsync(
        string eventType,
        string severity,
        string message,
        string traceId,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken);
}

public interface IIdentitySecurityBanService
{
    Task<bool> HasActiveAsync(
        string banType,
        string target,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task CreateTemporaryAsync(
        IdentitySecurityBanCreateRecord record,
        CancellationToken cancellationToken);
}
