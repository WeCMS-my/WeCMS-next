using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Logs;

public static class SecurityEventEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .RequireAuthorization();

        group.MapGet("/security-events", ListAsync).RequirePermission(LogPermissions.SecurityEventList);
        group.MapGet("/security-events/{id:long}", DetailAsync).RequirePermission(LogPermissions.SecurityEventDetail);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<SecurityEventSummaryDto>>> ListAsync(int page, int pageSize, string? eventType, string? severity, string? user, string? ip, DateTimeOffset? from, DateTimeOffset? to, ILogService service, CancellationToken cancellationToken)
    {
        return ApiResult<PagedResult<SecurityEventSummaryDto>>.Ok(await service.ListSecurityEventsAsync(new SecurityEventListQuery(page, pageSize, eventType, severity, user, ip, from, to), cancellationToken));
    }

    private static async Task<ApiResult<SecurityEventDetailDto>> DetailAsync(long id, ILogService service, CancellationToken cancellationToken)
    {
        return ApiResult<SecurityEventDetailDto>.Ok(await service.GetSecurityEventAsync(id, cancellationToken));
    }
}
