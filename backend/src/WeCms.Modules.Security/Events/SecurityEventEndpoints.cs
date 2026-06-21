using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Security.Events;

public static class SecurityEventEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .WithEndpointModule("security")
            .RequireAuthorization();

        group.MapGet("/security-events", ListAsync).RequireEndpointPermission(SecurityEventPermissions.SecurityEventList);
        group.MapGet("/security-events/{id:long}", DetailAsync).RequireEndpointPermission(SecurityEventPermissions.SecurityEventDetail);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<SecurityEventSummaryDto>>> ListAsync(int page, int pageSize, string? eventType, string? severity, string? user, string? ip, DateTimeOffset? from, DateTimeOffset? to, ISecurityEventService service, CancellationToken cancellationToken)
    {
        return ApiResult<PagedResult<SecurityEventSummaryDto>>.Ok(await service.ListSecurityEventsAsync(new SecurityEventListQuery(page, pageSize, eventType, severity, user, ip, from, to), cancellationToken));
    }

    private static async Task<ApiResult<SecurityEventDetailDto>> DetailAsync(long id, ISecurityEventService service, CancellationToken cancellationToken)
    {
        return ApiResult<SecurityEventDetailDto>.Ok(await service.GetSecurityEventAsync(id, cancellationToken));
    }
}
