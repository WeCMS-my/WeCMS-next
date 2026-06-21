namespace WeCms.Modules.Identity.Services;

public interface IAuthSecurityEventWriter
{
    Task RecordAsync(
        string eventType,
        long? userId,
        string? username,
        AuthRequestContext requestContext,
        string severity,
        string message,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed class AuthSecurityEventWriter : IAuthSecurityEventWriter
{
    private readonly IAuthRepository _repository;
    private readonly IIdentitySecurityAlertService _alertService;

    public AuthSecurityEventWriter(IAuthRepository repository, IIdentitySecurityAlertService alertService)
    {
        _repository = repository;
        _alertService = alertService;
    }

    public async Task RecordAsync(
        string eventType,
        long? userId,
        string? username,
        AuthRequestContext requestContext,
        string severity,
        string message,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var securityEvent = new SecurityEventRecord(
            eventType,
            userId,
            username,
            requestContext.Ip,
            severity,
            message,
            now,
            requestContext.TraceId);

        await _repository.RecordSecurityEventAsync(securityEvent, cancellationToken);
        await _alertService.PublishIfRequiredAsync(
            securityEvent.EventType,
            securityEvent.Severity,
            securityEvent.Message,
            securityEvent.TraceId ?? requestContext.TraceId,
            now,
            cancellationToken);
    }
}
