namespace WeCms.Modules.AccessControl.Records;

public enum PermissionCheckResult
{
    Allowed,
    UserDisabled,
    Forbidden
}

public sealed record PermissionUserRecord(long Id, string Status);

public sealed record PermissionSecurityEventRecord(
    string EventType,
    long? UserId,
    string? Username,
    string Ip,
    string Message,
    DateTimeOffset CreatedAt,
    string TraceId);

public interface IPermissionSecurityEventWriter
{
    Task RecordAsync(PermissionSecurityEventRecord record, CancellationToken cancellationToken);
}
