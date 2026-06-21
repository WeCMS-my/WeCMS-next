using WeCms.Shared;

namespace WeCms.Modules.Audit.Logs;

public interface ILogRepository
{
    Task<PagedResult<LoginLogSummaryDto>> ListLoginLogsAsync(LoginLogListCriteria criteria, CancellationToken cancellationToken);
    Task<LoginLogDetailDto?> GetLoginLogAsync(long id, CancellationToken cancellationToken);
    Task<PagedResult<AuditLogSummaryDto>> ListAuditLogsAsync(AuditLogListCriteria criteria, CancellationToken cancellationToken);
    Task<AuditLogDetailDto?> GetAuditLogAsync(long id, CancellationToken cancellationToken);
}
