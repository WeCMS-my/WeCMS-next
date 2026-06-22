namespace WeCms.Shared.Endpoints;

public sealed record AuditWriteRecord(
    string Module,
    string Resource,
    string Action,
    AuditWriteStatus Status,
    string RequestMethod,
    string RequestPath,
    string TraceId,
    string Detail = "",
    long? UserId = null,
    string? Username = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? TargetId = null);

public enum AuditWriteStatus
{
    Started,
    Completed,
    Failed
}
