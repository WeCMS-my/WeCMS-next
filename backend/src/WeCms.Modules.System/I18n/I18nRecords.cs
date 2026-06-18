namespace WeCms.Modules.System.I18n;

public sealed record I18nMessageListCriteria(int Page, int PageSize, string? Locale, string? Module, string? Keyword, string? Status);

public sealed record I18nMessageCreateRecord(string Locale, string Module, string MessageKey, string MessageValue, string? Remark, string Status, DateTimeOffset Now);

public sealed record I18nMessageUpdateRecord(long Id, string Module, string MessageValue, string? Remark, string Status, DateTimeOffset Now);

public sealed record I18nPublicMessageRecord(string MessageKey, string MessageValue);

public sealed record I18nAuditRecord(long ActorUserId, string ActorUsername, string Action, long? TargetId, string Resource, string Ip, string UserAgent, string TraceId, string Result, string Detail, DateTimeOffset CreatedAt);

