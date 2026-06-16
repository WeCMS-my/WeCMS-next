namespace WeCms.Modules.System.Logs;

public sealed record LoginLogListCriteria(int Page, int PageSize, string? Username, string? Ip, string? Result, DateTimeOffset? From, DateTimeOffset? To);

public sealed record AuditLogListCriteria(int Page, int PageSize, string? User, string? Module, string? Resource, string? Action, string? Result, DateTimeOffset? From, DateTimeOffset? To);

public sealed record SecurityEventListCriteria(int Page, int PageSize, string? EventType, string? Severity, string? User, string? Ip, DateTimeOffset? From, DateTimeOffset? To);
