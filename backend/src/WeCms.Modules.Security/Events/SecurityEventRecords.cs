namespace WeCms.Modules.Security.Events;

public sealed record SecurityEventListCriteria(int Page, int PageSize, string? EventType, string? Severity, string? User, string? Ip, DateTimeOffset? From, DateTimeOffset? To);
