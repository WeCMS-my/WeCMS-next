namespace WeCms.Modules.Identity.Records;

public sealed record UserRequestContext(
    long ActorUserId,
    string ActorUsername,
    string Ip,
    string UserAgent,
    string TraceId,
    DateTimeOffset Now);

public sealed record UserListCriteria(
    int Page,
    int PageSize,
    string? Keyword,
    string? Status,
    long? DeptId);

public sealed record UserCreateRecord(
    string Username,
    string DisplayName,
    string PasswordHash,
    string? Email,
    string? Phone,
    long? DeptId,
    DateTimeOffset Now);

public sealed record UserUpdateRecord(
    long Id,
    string DisplayName,
    string? Email,
    string? Phone,
    long? DeptId,
    DateTimeOffset Now);

public sealed record UserAuditRecord(
    long ActorUserId,
    string ActorUsername,
    string Action,
    long TargetUserId,
    string Ip,
    string UserAgent,
    string TraceId,
    string Result,
    string Detail,
    DateTimeOffset CreatedAt);

public sealed record UserSecurityEventRecord(
    string EventType,
    long? UserId,
    string? Username,
    string Ip,
    string Severity,
    string Message,
    DateTimeOffset CreatedAt,
    string TraceId = "");
