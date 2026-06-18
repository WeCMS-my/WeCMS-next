namespace WeCms.Modules.System.Auth;

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

    public AuthSecurityEventWriter(IAuthRepository repository)
    {
        _repository = repository;
    }

    public Task RecordAsync(
        string eventType,
        long? userId,
        string? username,
        AuthRequestContext requestContext,
        string severity,
        string message,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return _repository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                eventType,
                userId,
                username,
                requestContext.Ip,
                severity,
                message,
                now,
                requestContext.TraceId),
            cancellationToken);
    }
}
