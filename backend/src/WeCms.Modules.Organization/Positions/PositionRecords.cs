namespace WeCms.Modules.Organization.Positions;

public sealed record PositionRequestContext(long ActorUserId, string ActorUsername, string Ip, string UserAgent, string TraceId, DateTimeOffset Now);

public sealed record PositionListCriteria(int Page, int PageSize, string? Keyword, string? Status);

public sealed record PositionCreateRecord(string Code, string Name, int SortOrder, string Status, DateTimeOffset Now);

public sealed record PositionUpdateRecord(long Id, string Name, int SortOrder, string Status, DateTimeOffset Now);

public sealed record PositionAuditRecord(long ActorUserId, string ActorUsername, string Action, long TargetPositionId, string Ip, string UserAgent, string TraceId, string Result, string Detail, DateTimeOffset Now);
