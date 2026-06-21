using WeCms.Modules.Identity.Services;
using WeCms.Modules.Security;

namespace WeCms.Api.Security;

public sealed class IdentitySecurityAlertServiceAdapter : IIdentitySecurityAlertService
{
    private readonly ISecurityAlertService _inner;

    public IdentitySecurityAlertServiceAdapter(ISecurityAlertService inner)
    {
        _inner = inner;
    }

    public Task PublishIfRequiredAsync(
        string eventType,
        string severity,
        string message,
        string traceId,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        return _inner.PublishIfRequiredAsync(
            SecurityAlertRecord.FromSecurityEvent(eventType, severity, message, traceId, createdAt),
            cancellationToken);
    }
}

public sealed class IdentitySecurityBanServiceAdapter : IIdentitySecurityBanService
{
    private readonly ISecurityBanService _inner;

    public IdentitySecurityBanServiceAdapter(ISecurityBanService inner)
    {
        _inner = inner;
    }

    public async Task<bool> HasActiveAsync(
        string banType,
        string target,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return await _inner.FindActiveAsync(banType, target, now, cancellationToken) is not null;
    }

    public Task CreateTemporaryAsync(
        IdentitySecurityBanCreateRecord record,
        CancellationToken cancellationToken)
    {
        return _inner.CreateTemporaryAsync(
            new CreateSecurityBanRecord(
                record.BanType,
                record.Target,
                record.Reason,
                record.Severity,
                record.Source,
                record.ExpiresAt,
                record.CreatedAt),
            cancellationToken);
    }
}
