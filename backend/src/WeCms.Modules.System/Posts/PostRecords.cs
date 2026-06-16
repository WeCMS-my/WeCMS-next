namespace WeCms.Modules.System.Posts;

public sealed record PostRequestContext(long ActorUserId, string ActorUsername, string Ip, string UserAgent, string TraceId, DateTimeOffset Now);

public sealed record PostListCriteria(int Page, int PageSize, string? Keyword, string? Status);

public sealed record PostCreateRecord(string Code, string Name, int SortOrder, string Status, DateTimeOffset Now);

public sealed record PostUpdateRecord(long Id, string Name, int SortOrder, string Status, DateTimeOffset Now);

public sealed record PostAuditRecord(long ActorUserId, string ActorUsername, string Action, long TargetPostId, string Ip, string UserAgent, string TraceId, string Result, string Detail, DateTimeOffset Now);
