namespace WeCms.Modules.System.Departments;

public sealed record DepartmentRequestContext(long ActorUserId, string ActorUsername, string Ip, string UserAgent, string TraceId, DateTimeOffset Now);

public sealed record DepartmentCreateRecord(long? ParentId, string Code, string Name, int SortOrder, string Status, DateTimeOffset Now);

public sealed record DepartmentUpdateRecord(long Id, long? ParentId, string Name, int SortOrder, string Status, DateTimeOffset Now);

public sealed record DepartmentAuditRecord(long ActorUserId, string ActorUsername, string Action, long TargetDepartmentId, string Ip, string UserAgent, string TraceId, string Result, string Detail, DateTimeOffset Now);
