namespace WeCms.Modules.AccessControl.Records;

public sealed record MenuRequestContext(long ActorUserId, string ActorUsername, string Ip, string UserAgent, string TraceId, DateTimeOffset Now);

public sealed record MenuCreateRecord(
    long? ParentId,
    string Type,
    string Code,
    string Path,
    string? Component,
    string Title,
    string? I18nKey,
    string? Icon,
    int Sort,
    bool Hidden,
    bool KeepAlive,
    string? ExternalUrl,
    string? PermissionCode,
    string Status,
    DateTimeOffset Now);

public sealed record MenuUpdateRecord(
    long Id,
    long? ParentId,
    string Type,
    string Path,
    string? Component,
    string Title,
    string? I18nKey,
    string? Icon,
    int Sort,
    bool Hidden,
    bool KeepAlive,
    string? ExternalUrl,
    string? PermissionCode,
    string Status,
    DateTimeOffset Now);

public sealed record MenuSortRecord(long Id, long? ParentId, int Sort, DateTimeOffset Now);

public sealed record MenuAuditRecord(long ActorUserId, string ActorUsername, string Action, long TargetMenuId, string Ip, string UserAgent, string TraceId, string Result, string Detail, DateTimeOffset Now);
