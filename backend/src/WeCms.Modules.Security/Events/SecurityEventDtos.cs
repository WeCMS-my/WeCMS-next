using WeCms.Shared;

namespace WeCms.Modules.Security.Events;

public sealed record SecurityEventListQuery(int Page = 1, int PageSize = 20, string? EventType = null, string? Severity = null, string? User = null, string? Ip = null, DateTimeOffset? From = null, DateTimeOffset? To = null);

public sealed record SecurityEventSummaryDto(long Id, string EventType, long? UserId, string? Username, string? Ip, string Severity, string Source, string TraceId, string Message, DateTimeOffset CreatedAt);

public sealed record SecurityEventDetailDto(long Id, string EventType, long? UserId, string? Username, string? Ip, string Severity, string Source, string TraceId, string Message, DateTimeOffset CreatedAt);

public interface ISecurityEventService
{
    Task<PagedResult<SecurityEventSummaryDto>> ListSecurityEventsAsync(SecurityEventListQuery query, CancellationToken cancellationToken);
    Task<SecurityEventDetailDto> GetSecurityEventAsync(long id, CancellationToken cancellationToken);
}
