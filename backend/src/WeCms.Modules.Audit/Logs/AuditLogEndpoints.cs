using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Audit.Logs;

public static class AuditLogEndpoints
{
    public static IEndpointRouteBuilder MapAuditLogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .WithEndpointModule("audit")
            .RequireAuthorization();

        group.MapGet("/audit-logs", ListAsync).RequireEndpointPermission(LogPermissions.AuditLogList);
        group.MapGet("/audit-logs/{id:long}", DetailAsync).RequireEndpointPermission(LogPermissions.AuditLogDetail);

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
