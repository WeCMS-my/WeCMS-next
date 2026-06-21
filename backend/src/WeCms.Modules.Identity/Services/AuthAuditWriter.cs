namespace WeCms.Modules.Identity.Services;

public interface IAuthAuditWriter
{
    Task RecordAsync(
        long? userId,
        string? username,
        string action,
        string result,
        string detail,
        string requestPath,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);
}

public sealed class AuthAuditWriter : IAuthAuditWriter
{
    private const string AuthAuditModule = "auth";
    private const string AuthAuditResource = "auth";

    private readonly IAuthRepository _repository;
    private readonly IAuthClock _clock;

    public AuthAuditWriter(IAuthRepository repository, IAuthClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public Task RecordAsync(
        long? userId,
        string? username,
        string action,
        string result,
        string detail,
        string requestPath,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return _repository.RecordAuditLogAsync(
            new AuditLogRecord(
                userId,
                username,
                AuthAuditModule,
                AuthAuditResource,
                action,
                username,
                "POST",
                requestPath,
                requestContext.Ip,
                requestContext.UserAgent,
                requestContext.TraceId,
                result,
                detail,
                _clock.UtcNow),
            cancellationToken);
    }
}
