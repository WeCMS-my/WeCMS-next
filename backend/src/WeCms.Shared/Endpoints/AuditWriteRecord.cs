namespace WeCms.Shared.Endpoints;

public sealed record AuditWriteRecord(
    string Module,
    string Resource,
    string Action,
    AuditWriteStatus Status,
    string RequestMethod,
    string RequestPath,
    string TraceId,
    string Detail = "");

public enum AuditWriteStatus
{
    Started,
    Completed,
    Failed
}
