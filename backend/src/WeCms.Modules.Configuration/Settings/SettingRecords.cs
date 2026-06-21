namespace WeCms.Modules.Configuration.Settings;

public sealed record SettingRequestContext(long ActorUserId, string ActorUsername, string Ip, string UserAgent, string TraceId, DateTimeOffset Now);

public sealed record SettingListCriteria(int Page, int PageSize, string? Keyword, string? GroupCode);

public sealed record SettingUpdateRecord(string Key, string? Value, long UpdatedBy, DateTimeOffset Now);

public sealed record SettingAuditRecord(long ActorUserId, string ActorUsername, string Action, string TargetKey, string Ip, string UserAgent, string TraceId, string Result, string Detail, DateTimeOffset Now);

public sealed record SettingSecurityEventRecord(string EventType, long UserId, string Username, string Ip, string Severity, string Message, DateTimeOffset CreatedAt, string TraceId = "");

public sealed record SettingDefinition(string Key, bool IsSensitive, bool IsReadonly, bool IsSecuritySensitive, string ValueType);
