using WeCms.Shared;

namespace WeCms.Modules.System.Logs;

public interface ILogRepository
{
    Task<PagedResult<LoginLogSummaryDto>> ListLoginLogsAsync(LoginLogListCriteria criteria, CancellationToken cancellationToken);
    Task<LoginLogDetailDto?> GetLoginLogAsync(long id, CancellationToken cancellationToken);
    Task<PagedResult<AuditLogSummaryDto>> ListAuditLogsAsync(AuditLogListCriteria criteria, CancellationToken cancellationToken);
    Task<AuditLogDetailDto?> GetAuditLogAsync(long id, CancellationToken cancellationToken);
    Task<PagedResult<SecurityEventSummaryDto>> ListSecurityEventsAsync(SecurityEventListCriteria criteria, CancellationToken cancellationToken);
    Task<SecurityEventDetailDto?> GetSecurityEventAsync(long id, CancellationToken cancellationToken);
}
