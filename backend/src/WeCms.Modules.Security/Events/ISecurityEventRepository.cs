using WeCms.Shared;

namespace WeCms.Modules.Security.Events;

public interface ISecurityEventRepository
{
    Task<PagedResult<SecurityEventSummaryDto>> ListSecurityEventsAsync(SecurityEventListCriteria criteria, CancellationToken cancellationToken);
    Task<SecurityEventDetailDto?> GetSecurityEventAsync(long id, CancellationToken cancellationToken);
}
