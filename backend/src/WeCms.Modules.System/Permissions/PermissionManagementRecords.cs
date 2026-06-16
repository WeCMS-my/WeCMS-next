namespace WeCms.Modules.System.Permissions;

public sealed record PermissionRequestContext(long ActorUserId, string ActorUsername, string Ip, string UserAgent, string TraceId, DateTimeOffset Now);

public sealed record PermissionCreateRecord(string Code, string Name, string Module, string? Description, DateTimeOffset Now);

public sealed record PermissionUpdateRecord(long Id, string Name, string Module, string? Description, DateTimeOffset Now);

public sealed record PermissionAuditRecord(long ActorUserId, string ActorUsername, string Action, long TargetPermissionId, string Ip, string UserAgent, string TraceId, string Result, string Detail, DateTimeOffset Now);
