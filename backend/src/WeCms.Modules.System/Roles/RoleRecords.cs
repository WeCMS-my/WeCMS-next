namespace WeCms.Modules.System.Roles;

public sealed record RoleRequestContext(
    long ActorUserId,
    string ActorUsername,
    string Ip,
    string UserAgent,
    string TraceId,
    DateTimeOffset Now);

public sealed record RoleListCriteria(
    int Page,
    int PageSize,
    string? Keyword,
    string? Status);

public sealed record RoleCreateRecord(
    string Code,
    string Name,
    DateTimeOffset Now);

public sealed record RoleUpdateRecord(
    long Id,
    string Name,
    DateTimeOffset Now);

public sealed record RoleAuditRecord(
    long ActorUserId,
    string ActorUsername,
    string Action,
    long TargetRoleId,
    string Ip,
    string UserAgent,
    string TraceId,
    string Result,
    string Detail,
    DateTimeOffset Now);
