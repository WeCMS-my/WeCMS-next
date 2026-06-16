using WeCms.Shared;

namespace WeCms.Modules.System.Logs;

public sealed record LoginLogListQuery(int Page = 1, int PageSize = 20, string? Username = null, string? Ip = null, string? Result = null, DateTimeOffset? From = null, DateTimeOffset? To = null);

public sealed record LoginLogSummaryDto(long Id, string Username, long? UserId, string? Ip, string? Result, string? Reason, DateTimeOffset CreatedAt);

public sealed record LoginLogDetailDto(long Id, string Username, long? UserId, string? Ip, string? UserAgent, string Result, string? Reason, DateTimeOffset CreatedAt);

public sealed record AuditLogListQuery(int Page = 1, int PageSize = 20, string? User = null, string? Module = null, string? Resource = null, string? Action = null, string? Result = null, DateTimeOffset? From = null, DateTimeOffset? To = null);

public sealed record AuditLogSummaryDto(long Id, long? UserId, string? Username, string Module, string Resource, string Action, string? TargetId, string Result, DateTimeOffset CreatedAt);

public sealed record AuditLogDetailDto(long Id, long? UserId, string? Username, string Module, string Resource, string Action, string? TargetId, string RequestMethod, string RequestPath, string? IpAddress, string? UserAgent, string? TraceId, string Result, string Detail, DateTimeOffset CreatedAt);

public sealed record SecurityEventListQuery(int Page = 1, int PageSize = 20, string? EventType = null, string? Severity = null, string? User = null, string? Ip = null, DateTimeOffset? From = null, DateTimeOffset? To = null);

public sealed record SecurityEventSummaryDto(long Id, string EventType, long? UserId, string? Username, string? Ip, string Severity, string Message, DateTimeOffset CreatedAt);

public sealed record SecurityEventDetailDto(long Id, string EventType, long? UserId, string? Username, string? Ip, string Severity, string Message, DateTimeOffset CreatedAt);

public interface ILogService
{
    Task<PagedResult<LoginLogSummaryDto>> ListLoginLogsAsync(LoginLogListQuery query, CancellationToken cancellationToken);
    Task<LoginLogDetailDto> GetLoginLogAsync(long id, CancellationToken cancellationToken);
    Task<PagedResult<AuditLogSummaryDto>> ListAuditLogsAsync(AuditLogListQuery query, CancellationToken cancellationToken);
    Task<AuditLogDetailDto> GetAuditLogAsync(long id, CancellationToken cancellationToken);
    Task<PagedResult<SecurityEventSummaryDto>> ListSecurityEventsAsync(SecurityEventListQuery query, CancellationToken cancellationToken);
    Task<SecurityEventDetailDto> GetSecurityEventAsync(long id, CancellationToken cancellationToken);
}
