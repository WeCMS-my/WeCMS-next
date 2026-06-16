namespace WeCms.Modules.System.Dicts;

public sealed record DictRequestContext(long ActorUserId, string ActorUsername, string Ip, string UserAgent, string TraceId, DateTimeOffset Now);

public sealed record DictTypeListCriteria(int Page, int PageSize, string? Keyword, string? Status);

public sealed record DictTypeCreateRecord(string Code, string Name, string? Description, int SortOrder, string Status, DateTimeOffset Now);

public sealed record DictTypeUpdateRecord(long Id, string Name, string? Description, int SortOrder, string Status, DateTimeOffset Now);

public sealed record DictValueCreateRecord(long TypeId, string Label, string Value, string? Description, int SortOrder, bool IsDefault, string Status, DateTimeOffset Now);

public sealed record DictValueUpdateRecord(long Id, string Label, string Value, string? Description, int SortOrder, bool IsDefault, string Status, DateTimeOffset Now);

public sealed record DictAuditRecord(long ActorUserId, string ActorUsername, string Action, long TargetId, string Resource, string Ip, string UserAgent, string TraceId, string Result, string Detail, DateTimeOffset Now);
