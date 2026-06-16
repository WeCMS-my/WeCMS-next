using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Logs;

public static class AuditLogEndpoints
{
    public static IEndpointRouteBuilder MapAuditLogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .RequireAuthorization();

        group.MapGet("/audit-logs", ListAsync).RequirePermission(LogPermissions.AuditLogList);
        group.MapGet("/audit-logs/{id:long}", DetailAsync).RequirePermission(LogPermissions.AuditLogDetail);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<AuditLogSummaryDto>>> ListAsync(int page, int pageSize, string? user, string? module, string? resource, string? action, string? result, DateTimeOffset? from, DateTimeOffset? to, ILogService service, CancellationToken cancellationToken)
    {
        return ApiResult<PagedResult<AuditLogSummaryDto>>.Ok(await service.ListAuditLogsAsync(new AuditLogListQuery(page, pageSize, user, module, resource, action, result, from, to), cancellationToken));
    }

    private static async Task<ApiResult<AuditLogDetailDto>> DetailAsync(long id, ILogService service, CancellationToken cancellationToken)
    {
        return ApiResult<AuditLogDetailDto>.Ok(await service.GetAuditLogAsync(id, cancellationToken));
    }
}
