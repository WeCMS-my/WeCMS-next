namespace WeCms.Modules.Audit.Logs;

public sealed record LoginLogListCriteria(int Page, int PageSize, string? Username, string? Ip, string? Result, DateTimeOffset? From, DateTimeOffset? To);

public sealed record AuditLogListCriteria(int Page, int PageSize, string? User, string? Module, string? Resource, string? Action, string? Result, DateTimeOffset? From, DateTimeOffset? To);
